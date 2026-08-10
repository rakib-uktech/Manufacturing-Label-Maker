using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetSuite;
using Newtonsoft.Json;
using Npgsql;
using System.Data;

namespace TPLPLM.Pages
{
    public class Label_PrintModel : PageModel
    {
        private readonly NetSuiteClient _netSuiteClient;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string AssemblyItemId { get; set; } // Input for fetching a single assembly item
        public AssemblyItem AssemblyItem { get; set; } // Hold the data for a single item
        public string ErrorMessage { get; set; } // Error messages for display
        public string AssemblyItemJson { get; set; } // JSON representation of the assembly item

        [BindProperty]
        public string WorkOrderId { get; set; } // Input for fetching a single work order
        public WorkOrder WorkOrder { get; set; } // Hold the data for a single work order
        public string WorkOrderJson { get; set; } // JSON representation of the work order

        public string errorMessage = "";
        public string successMessage = "";
        public LabelInfo labelinfo = new LabelInfo();
        public string DefaultConnection { get; private set; }
        public string _connectionString { get; private set; }

        public Label_PrintModel(NetSuiteClient netSuiteClient, IConfiguration configuration)
        {
            _netSuiteClient = netSuiteClient;
            _configuration = configuration;
            WorkOrder = new WorkOrder(); // Ensure it's not null
        }

        public async Task OnGetAsync(string id = null)
        {
            string lid = Request.Query["intid"];
            int label_id = Int32.Parse(lid);

            try
            {
                string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

                using (NpgsqlConnection connection = new NpgsqlConnection(DefaultConnection))
                {
                    connection.Open();
                    string sql = "SELECT * FROM label_Count WHERE id=@id";

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", label_id);
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                labelinfo.id = reader.GetInt32("id");
                                labelinfo.Create_Date = reader.GetDateTime("Create_Date").ToString("MMMM yyyy");
                                labelinfo.Best_Before = reader.GetDateTime("Create_Date").AddYears(2).ToString("MMMM yyyy");
                                labelinfo.Expiry = reader.GetDateTime("Create_Date").AddYears(2).ToString("yyMMdd");
                                labelinfo.Label_Type = reader.GetString("Label_Type");
                                labelinfo.Product = reader.GetString("Product");
                                labelinfo.Description = reader.GetString("Description");
                                labelinfo.Label_Qty = reader.GetInt32("Label_Qty");
                                labelinfo.Label_No = reader.GetString("Label_No");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    AssemblyItemId = id; // Assign the provided ID
                    AssemblyItem = await _netSuiteClient.GetAssemblyItemAsync(id); // Fetch item by ID

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
                if (ex.Message.Contains("401"))
                {
                    ErrorMessage = "Unauthorized access. Please verify your NetSuite API credentials and permissions.";
                }
                else
                {
                    ErrorMessage = $"An error occurred: {ex.Message}";
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AssemblyItemId))
                {
                    AssemblyItem = await _netSuiteClient.GetAssemblyItemAsync(AssemblyItemId);

                    if (AssemblyItem != null)
                    {
                        AssemblyItemJson = JsonConvert.SerializeObject(AssemblyItem, Formatting.Indented);
                    }
                    else
                    {
                        ErrorMessage = $"No data found for assembly item ID: {AssemblyItemId}";
                    }
                }
                else
                {
                    ErrorMessage = "Please provide an Assembly Item ID.";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("401"))
                {
                    ErrorMessage = "Unauthorized access. Please verify your NetSuite API credentials and permissions.";
                }
                else
                {
                    ErrorMessage = $"An error occurred: {ex.Message}";
                }
            }

            return Page();
        }

        
    }

}




