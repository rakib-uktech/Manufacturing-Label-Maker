using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using NetSuite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Collections.Generic;
using System.Composition;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Globalization;
using System.Net;
using System.Reflection;
using TPLPLM.Pages;
using TPLPLM.PrintTemplates;
using ZXing;
using ZXing.Rendering;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace TPLPLM.Pages
{

    public class PrintDuplicateModel : PageModel
    {
        private readonly NetSuiteClient _netSuiteClient;
        private readonly IConfiguration _configuration;

        public PrintDuplicateModel(NetSuiteClient netSuiteClient, IConfiguration configuration)
        {
            _netSuiteClient = netSuiteClient;
            _configuration = configuration;
        }


        [BindProperty]
        public string AssemblyItemId { get; set; }


        [BindProperty]
        public AssemblyItem AssemblyItem { get; set; }

        public string AssemblyItemJson { get; set; }

        [BindProperty]
        public string WorkOrderId { get; set; }

        public WorkOrder WorkOrder { get; set; } = new WorkOrder();
        
        [BindProperty]   // ✅ ADD IT HERE
        public LabelInfo labelinfo { get; set; } = new LabelInfo();

        [BindProperty]
        public string Address { get; set; }

        [BindProperty]
        public string SelectedPrinter { get; set; }
        [BindProperty]
        public string SelectedLabelTemplate { get; set; }

        public List<string> AvailablePrinters { get; set; }
        public string DefaultPrinter { get; set; }

        public List<string> LabelTemplates { get; set; } = new List<string>();

        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        // -------------------------------
        // Helper Method: Get Label Templates from NetSuite
        // -------------------------------
        private async Task<List<string>> GetLabelTemplatesAsync()
        {
            var templates = new List<string>();
            string connectionString = _configuration.GetConnectionString("NetSuiteOdbc");

            using var conn = new OdbcConnection(connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT name 
                FROM customlist1541
                WHERE isinactive = 'F'
                ORDER BY name ASC";

            using var cmd = new OdbcCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    templates.Add(reader.GetString(0).Trim());
            }

            return templates;
        }

        // -------------------------------
        // GET Handler
        // -------------------------------
        public async Task OnGetAsync(string id = null)
        {
            string lid = Request.Query["intid"];
            if (!int.TryParse(lid, out int label_id))
            {
                ErrorMessage = "Invalid label ID.";
                return;
            }

            DefaultPrinter = new PrinterSettings().PrinterName;
            AvailablePrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            SelectedPrinter = DefaultPrinter;

            // Fetch label info from PostgreSQL
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connString);
                connection.Open();

                string sql = "SELECT * FROM label_Count WHERE id = @id";
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", label_id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var createDate = reader.GetDateTime(reader.GetOrdinal("Create_Date"));
                    labelinfo = new LabelInfo
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id")),
                        Create_Date = createDate.ToString("dd/MM/yy"),
                        Best_Before = createDate.AddYears(1).ToString("dd/MM/yy"),
                        Expiry = createDate.ToString("yyMMdd"),
                        Label_Type = reader.GetString(reader.GetOrdinal("Label_Type")),
                        Product = reader.GetString(reader.GetOrdinal("Product")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Label_Qty = reader.GetInt32(reader.GetOrdinal("Label_Qty")),
                        Build_Qty = reader.GetInt32(reader.GetOrdinal("Build_Qty")),
                        Label_No = reader.GetString(reader.GetOrdinal("Label_No")),
                        Work_Order = reader.GetString(reader.GetOrdinal("Work_Order"))
                    };
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
                return;
            }

            // Fetch label templates from NetSuite
            try
            {
                LabelTemplates = await GetLabelTemplatesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load label templates from NetSuite: {ex.Message}";
            }

            // Fetch Assembly Item from NetSuite
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    AssemblyItemId = id;
                    AssemblyItem = await _netSuiteClient.GetAssemblyItemAsync(id);

                    if (AssemblyItem != null)
                    {
                        AssemblyItemJson = JsonConvert.SerializeObject(AssemblyItem, Formatting.Indented);
                    }
                    else
                    {
                        ErrorMessage = $"No data found for assembly item ID: {id}";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.Contains("401")
                    ? "Unauthorized access. Check NetSuite API credentials."
                    : $"NetSuite error: {ex.Message}";
            }

            if (labelinfo.Label_Type == "Pallet")
            {
                SelectedLabelTemplate = AssemblyItem.Custitemproduct_Spec_Pallet_Label_Temp;
            }
            else
            {
                SelectedLabelTemplate = AssemblyItem.Custitemproduct_Spec_Label_Temp;
            }
        }

        public async Task<IActionResult> OnPostAsync(string id = null)
        {
           
            // 1. Parse and validate label ID
            if (!int.TryParse(Request.Query["intid"], out int label_id))
            {
                ErrorMessage = "Invalid label ID.";
                return Page();
            }

            if (AssemblyItem == null)
            {
                AssemblyItem = new AssemblyItem();
            }

            // 2. Setup printers
            DefaultPrinter = new PrinterSettings().PrinterName;
            AvailablePrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();

            if (string.IsNullOrEmpty(SelectedPrinter))
                SelectedPrinter = DefaultPrinter;

            if (string.IsNullOrEmpty(SelectedPrinter))
            {
                ErrorMessage = "No printer selected.";
                return Page();
            }
            // Fetch label templates from NetSuite
            try
            {
                LabelTemplates = await GetLabelTemplatesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load label templates from NetSuite: {ex.Message}";
            }


            // 4. Fetch Assembly Item ONLY if mandatory fields are missing
            try
            {
                bool fetchFromNetSuite = false;

                // Example: mandatory fields
                if (AssemblyItem == null
                    || string.IsNullOrWhiteSpace(AssemblyItem.ItemId)
                    || string.IsNullOrWhiteSpace(AssemblyItem.Description)
                    || string.IsNullOrWhiteSpace(AssemblyItem.Custitemproduct_Spec_Sku)
                    || string.IsNullOrWhiteSpace(AssemblyItem.Custitem12))
                    
                {
                    fetchFromNetSuite = true;
                }

                if (!string.IsNullOrEmpty(id) && fetchFromNetSuite)
                {
                    AssemblyItemId = id;
                    var fetchedItem = await _netSuiteClient.GetAssemblyItemAsync(id);

                    if (fetchedItem == null)
                    {
                        ErrorMessage = $"No data found for assembly item ID: {id}";
                        return Page();
                    }

                    // Only overwrite missing fields
                    AssemblyItem.ItemId ??= fetchedItem.ItemId;
                    AssemblyItem.Description ??= fetchedItem.Description;
                    AssemblyItem.Custitemproduct_Spec_Sku ??= fetchedItem.Custitemproduct_Spec_Sku;
                    AssemblyItem.Custitem12 ??= fetchedItem.Custitem12;
                    AssemblyItem.Custitem13 ??= fetchedItem.Custitem13;
                    AssemblyItem.Custitemproduct_Spec_Qtyperpallet ??= fetchedItem.Custitemproduct_Spec_Qtyperpallet;
                    AssemblyItem.Custitemproduct_Spec_Caseperpallet ??= fetchedItem.Custitemproduct_Spec_Caseperpallet;
                    AssemblyItem.Custitemproduct_Spec_Casewtnet ??= fetchedItem.Custitemproduct_Spec_Palletwtnetkg;
                    AssemblyItem.Custitemproduct_Spec_Casewtgrosskg ??= fetchedItem.Custitemproduct_Spec_Palletwtgrosskg;
                    AssemblyItem.Custitemproduct_Spec_Palletwtnetkg ??= fetchedItem.Custitemproduct_Spec_Palletwtnetkg;
                    AssemblyItem.Custitemproduct_Spec_Palletwtgrosskg ??= fetchedItem.Custitemproduct_Spec_Palletwtgrosskg;
                    AssemblyItem.Custitemproduct_Spec_Label_Temp ??= fetchedItem.Custitemproduct_Spec_Label_Temp;
                    // Add more fields as needed

                    AssemblyItemJson = JsonConvert.SerializeObject(AssemblyItem, Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.Contains("401")
                    ? "Unauthorized access. Check NetSuite API credentials."
                    : $"NetSuite error: {ex.Message}";
                return Page();
            }

            // 5. Validate label info for printing
            if (string.IsNullOrEmpty(labelinfo?.Label_No) || !long.TryParse(labelinfo.Label_No, out long labelNoBase))
            {
                ErrorMessage = "Invalid Label Number format for printing.";
                return Page();
            }



            // 6. Print labels
            // Case Label Print--------------------------------
            if (labelinfo.Label_Type == "Case")
            {
                switch (SelectedLabelTemplate)
                {
                    case "4x6 Standard Case Label":
                        new CaseLabel_4x6_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Standard Case Label":
                        new CaseLabel_100x100_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Standard Case Label (Without Logo)":
                        new CaseLabel_100x100_WithoutLogo_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Odeon Case Label":
                        new CaseLabel_100x100_Odeon_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 AXB Group Case Label":
                        new CaseLabel_100x100_AXB_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 R&R Packaging Case Label":
                        new CaseLabel_100x100_RR_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 BIDFOOD Case Label":
                        new CaseLabel_100x100_BIDFOOD_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 150 PEPSI Case Label":
                        new CaseLabel_PEPSI_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Down2Earth Case Label(Cup)":
                        new CaseLabel_100x100_down2earth_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Down2Earth Case Label(Straw)":
                        new CaseLabel_100x100_down2earth_Template_Straw().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;
                    
                    case "100 x 100 Down2Earth Case Label(NoBarCode)":
                        new CaseLabel_100x100_down2earth_Template_NoBarCode().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "100 x 100 Lilly Packaging Case Label":
                        new CaseLabel_100x100_Lilly_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;
                    case "100 x 100 Daylesford Case Label":
                        new CaseLabel_100x100_Daylesford_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    default:
                        throw new Exception($"Unknown label template: {SelectedLabelTemplate}");
                }
            }

            // SBUX  Case Label Print--------------------------------
            if (labelinfo.Label_Type == "SBUX")
            {
                new CaseLabel_SBUX_Template().Print(
                    labelinfo,
                    AssemblyItem,
                    SelectedPrinter,
                    labelNoBase
                    );

            }

            // Pallet Label Print--------------------------------
            if (labelinfo.Label_Type == "Pallet")
            {
                switch (SelectedLabelTemplate)
                {
                    case "150 x 200 Starbucks Pallet Label":
                        new PalletLabel_150x200_Starbucks_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "150 x 200 Standard Pallet Label":
                        new PalletLabel_150x200_Standard_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "150 x 200 AXB Group Pallet Label":
                        new PalletLabel_150x200_AXBGroup_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "150 x 200 Standard Pallet Label (Without Logo)":
                        new PalletLabel_150x200_Standard_NoLogo_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;
                    // ✅ NEW PEPSI CASE
                    case "150 x 200 Pepsi Pallet Label":
                        new PalletLabel_150x200_Pepsi_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;
                    case "150 x 200 Down2Earth Pallet Label(Cup)":
                        new PalletLabel_150x200_Down2Earth_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "150 x 200 Down2Earth Pallet Label(Straw)":
                        new PalletLabel_150x200_Down2Earth_Template_Straw().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    case "150 x 200 Lilly Packaging Pallet Label":
                        new PalletLabel_150x200_LillyPackaging_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;
                    case "150 x 200 Daylesford Pallet Label":
                        new PalletLabel_150x200_Daylesford_Template().Print(
                            labelinfo,
                            AssemblyItem,
                            SelectedPrinter,
                            Address,
                            labelNoBase
                        );
                        break;

                    default:
                        ErrorMessage = "Invalid label template selected.";
                        break;
                }

            }

            // KBA Label Print--------------------------------
            if (labelinfo.Label_Type == "KBA")
            {
                new CaseLabel_KBA_Template().Print(
                        labelinfo,
                        AssemblyItem,
                        SelectedPrinter,
                        Address,
                        labelNoBase
                    );

            }

            // CC Label Print--------------------------------
            if (labelinfo.Label_Type == "CC")
            {
                new CaseLabel_CC_Template().Print(
                    labelinfo,
                    AssemblyItem,
                    SelectedPrinter,
                    Address,
                    labelNoBase
                );

            }

            return Page();

        }
       
        private string CalculateSSCCWithCheckDigit(string ssccWithoutCheckDigit)
        {
            ssccWithoutCheckDigit = new string(ssccWithoutCheckDigit.Where(char.IsDigit).ToArray());

            int sum = 0;
            bool multiplyBy3 = true;

            for (int i = ssccWithoutCheckDigit.Length - 1; i >= 0; i--)
            {
                int digit = ssccWithoutCheckDigit[i] - '0';
                sum += digit * (multiplyBy3 ? 3 : 1);
                multiplyBy3 = !multiplyBy3;
            }

            int mod = sum % 10;
            int checkDigit = (10 - mod) % 10;

            return ssccWithoutCheckDigit + checkDigit.ToString();
        }
        private string AddEan13CheckDigit(string twelveDigits)
        {
            if (twelveDigits.Length != 12 || !twelveDigits.All(char.IsDigit))
                throw new ArgumentException("EAN-13 base must be 12 numeric digits.");

            int sum = 0;

            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(twelveDigits[i].ToString());

                // Even index (0-based) = weight 1
                // Odd index = weight 3
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            return twelveDigits + checkDigit.ToString();
        }

    }
}
