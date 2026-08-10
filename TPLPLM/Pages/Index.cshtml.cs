using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using NetSuite;
using Newtonsoft.Json;
using System.Diagnostics;

namespace TPLPLM.Pages
{

    [Authorize(Roles = "Admin,Supervisor,Management")]
    public class IndexModel : PageModel
    {
        public String errorMessage = "";
        public String successMessage = "";
        public LabelInfo labelinfo = new LabelInfo();
        private readonly IConfiguration _configuration;
        public string DefaultConnection { get; private set; }
        public string _connectionString { get; private set; }
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<LabelInfo> listLabel = new List<LabelInfo>();
        public List<LabelInfo> LabelTypelist = new List<LabelInfo>();
        public List<LabelInfo> Productlist = new List<LabelInfo>();
        public List<LabelInfo> Descriptionlist = new List<LabelInfo>();
        public List<LabelInfo> PalletCount = new List<LabelInfo>();
        public List<LabelInfo> CaseCount = new List<LabelInfo>();
        public List<LabelInfo> KbaCount = new List<LabelInfo>();
        public List<LabelInfo> CCCount = new List<LabelInfo>();
        public List<LabelInfo> ShippingCount = new List<LabelInfo>();
        public List<LabelInfo> SBUXCount = new List<LabelInfo>();
        public List<LabelInfo> SalesOrderList = new List<LabelInfo>();

        string st_date = DateTime.Now.ToString("yyyy") + "-" + "01" + "-" + "01";
        string end_date = DateTime.Now.ToString("yyyy-MM-dd");
        string Label_Ref = "";
        string SSCC_Ref = "";
        int insertedId;


        public void OnGet()
        {
           
            try
            {
                string _connectionString = _configuration.GetConnectionString("NetSuiteOdbc");

                using (OdbcConnection NetSuiteConnection = new OdbcConnection(_connectionString))
                {
                    NetSuiteConnection.Open();

                    Productlist.Clear();
                    string lastMonthDate = DateTime.Now.AddMonths(-2).ToString("yyyy-MM-dd");
                    string last3MonthDate = DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd");

                    string sql = $@"
                                    SELECT DISTINCT 
                                        a.tranid, 
                                        c.id AS intid,      
                                        c.itemid, 
                                        c.displayname,
                                        c.custitem13
                                    FROM 
                                        transaction a
                                    JOIN 
                                        transactionline b ON a.id = b.transaction
                                    JOIN 
                                        item c ON b.item = c.id
                                    WHERE 
                                        RTRIM(a.recordtype) = 'workorder'
                                        AND b.mainline = 'T'
                                        AND b.itemtype = 'Assembly'
                                        AND b.item IS NOT NULL
                                        AND a.createddate >= {{d '{lastMonthDate}'}}
                                    ORDER BY 
                                        a.tranid DESC";

                    using (OdbcCommand command = new OdbcCommand(sql, NetSuiteConnection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var labelinfo = new LabelInfo
                                {
                                    intid = reader.GetInt32(reader.GetOrdinal("intid")),
                                    Customer = !reader.IsDBNull(reader.GetOrdinal("custitem13")) ? reader["custitem13"].ToString() : "Unknown",
                                    Ref_No = reader["tranid"].ToString(),                                    
                                    Product = !reader.IsDBNull(reader.GetOrdinal("itemid")) ? reader["itemid"].ToString() : "Unknown",
                                    Description = !reader.IsDBNull(reader.GetOrdinal("displayname")) ? reader["displayname"].ToString() : "No Description"
                                };

                                Productlist.Add(labelinfo);

                            }
                            reader.Close();
                        }
                    }
                    SalesOrderList.Clear();

                    sql = $@"
                        SELECT DISTINCT
                            a.id AS intid,
                            a.tranid,
                            a.shipaddress
                        FROM
                            transaction a
                        WHERE
                            RTRIM(a.recordtype) = 'salesorder'
                            AND a.createddate >= {{d '{last3MonthDate}'}}
                        ORDER BY
                            a.tranid DESC";

                    using (OdbcCommand command = new OdbcCommand(sql, NetSuiteConnection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SalesOrderList.Add(new LabelInfo
                                {
                                    SalesOrderInternalId = reader.GetInt32(reader.GetOrdinal("intid")),
                                    SalesOrder = reader["tranid"].ToString(),
                                    ShipAddress = reader["shipaddress"].ToString()
                                });
                            }
                        }
                    }

                    string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

                    using (NpgsqlConnection connection = new NpgsqlConnection(DefaultConnection))
                    {
                        connection.Open();

                        sql = @"
                            SELECT id,create_date,label_type,work_order,intid,product,description,label_no,label_qty, build_qty, comment
                            FROM label_Count ORDER BY id DESC";

                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                  
                                    var labelinfo = new LabelInfo
                                    {
                                        id = reader.GetInt32(reader.GetOrdinal("id")),
                                        Create_Date = reader.GetDateTime(reader.GetOrdinal("create_date")).ToString("dd/MM/yyyy"),
                                        Label_Type = reader.GetString(reader.GetOrdinal("label_type")),
                                        Work_Order = reader.GetString(reader.GetOrdinal("Work_Order")),
                                        intid = reader.GetInt32(reader.GetOrdinal("intid")),
                                        Product = reader.GetString(reader.GetOrdinal("product")),
                                        Description = reader.GetString(reader.GetOrdinal("description")),
                                        Label_Qty = reader.GetInt32(reader.GetOrdinal("label_qty")),
                                        Build_Qty = reader.GetInt32(reader.GetOrdinal("build_qty")),
                                        Label_No = reader.GetString(reader.GetOrdinal("label_no")),
                                        // ✅ ADD THIS
                                        Comment = reader.IsDBNull(reader.GetOrdinal("comment"))
                                        ? ""
                                        : reader.GetString("comment"),
                                    };

                                    listLabel.Add(labelinfo);
                                }
                            }

                        }
                        sql = "SELECT DISTINCT Label_Type FROM label_Count WHERE label_Count IS NOT NULL";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo labelType = new LabelInfo();
                                    labelType.Label_Type = reader.GetString("Label_Type");
                                    LabelTypelist.Add(labelType);
                                }
                                reader.Close();
                            }
                        }

                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count WHERE Label_Type IN ('Case', 'SBUX');";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo casecount = new LabelInfo();
                                    casecount.CaseCount = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "01" + Convert.ToInt32(reader.GetValue(0)).ToString("00000#");
                                    CaseCount.Add(casecount);
                                }
                                reader.Close();
                            }
                        }

                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count WHERE Label_Type IN ('Case', 'SBUX');";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo sbuxcount = new LabelInfo();
                                    sbuxcount.SBUXCount = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "01" + Convert.ToInt32(reader.GetValue(0)).ToString("00000#");
                                    SBUXCount.Add(sbuxcount);
                                }
                                reader.Close();
                            }
                        }

                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count Where Label_Type = '" + "Pallet" + "';";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo palletcount = new LabelInfo();
                                    palletcount.PalletCount = "0506062281" + Convert.ToInt32(reader.GetValue(0)).ToString("000000#");
                                    PalletCount.Add(palletcount);
                                }
                                reader.Close();
                            }
                        }
                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count Where Label_Type = '" + "KBA" + "';";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo kbacount = new LabelInfo();
                                    kbacount.KbaCount = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "03" + Convert.ToInt32(reader.GetValue(0)).ToString("00000#");
                                    KbaCount.Add(kbacount);
                                }
                                reader.Close();
                            }
                        }
                        
                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count Where Label_Type = '" + "Shipping" + "';";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo shippingcount = new LabelInfo();
                                    shippingcount.ShippingCount = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "05" + Convert.ToInt32(reader.GetValue(0)).ToString("00000#");
                                    ShippingCount.Add(shippingcount);
                                }
                                reader.Close();
                            }
                        }
                        sql = "SELECT SUM(label_qty) AS Qty FROM label_Count Where Label_Type = '" + "CC" + "';";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    LabelInfo cccount = new LabelInfo();
                                    cccount.CCCount = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "04" + Convert.ToInt32(reader.GetValue(0)).ToString("00000#");
                                    CCCount.Add(cccount);
                                }
                                reader.Close();
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
        }

        public IActionResult OnPost()
        {
            labelinfo.SalesOrder = Request.Form["SalesOrder"];
            labelinfo.SalesOrderInternalId = string.IsNullOrWhiteSpace(Request.Form["SalesOrderInternalId"])
                ? 0
                : Convert.ToInt32(Request.Form["SalesOrderInternalId"]);

            labelinfo.Comment = Request.Form["ShipAddress"];
            labelinfo.Work_Order = Request.Form["WorkOrder"];
            labelinfo.Label_Type = Request.Form["LabelType"];
            labelinfo.intid = Convert.ToInt32(Request.Form["Internal_Id"]);
            labelinfo.Product = Request.Form["Item_No"];
            labelinfo.Description = Request.Form["Item_Name"];
            labelinfo.Label_Qty = Convert.ToInt32(Request.Form["Label_Qty"]);
            labelinfo.Build_Qty = Convert.ToInt32(Request.Form["Build_Qty"]);

            string sqlString;

            if (labelinfo.Label_Type == "Case" || labelinfo.Label_Type == "SBUX")
            {
                sqlString = "SELECT SUM(label_qty) AS Qty FROM label_Count " +
                            "WHERE Label_Type IN ('Case', 'SBUX') " +
                            "AND Create_Date BETWEEN '" + st_date + "' AND '" + end_date + "'";
            }
            else
            {
                sqlString = "SELECT SUM(label_qty) AS Qty FROM label_Count " +
                            "WHERE Label_Type = '" + labelinfo.Label_Type + "' " +
                            "AND Create_Date BETWEEN '" + st_date + "' AND '" + end_date + "'";
            }


            string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(DefaultConnection))
                {
                    connection.Open();

                    using (NpgsqlCommand sqlCmd = new NpgsqlCommand(sqlString, connection))
                    {
                        using (NpgsqlDataReader reader = sqlCmd.ExecuteReader())
                        {
                            int currentQty = 0;
                            if (reader.Read() && !reader.IsDBNull(0))
                            {
                                currentQty = reader.GetInt32(0);
                            }

                            // Generate Label_Ref
                            switch (labelinfo.Label_Type)
                            {
                                case "Pallet":
                                    Label_Ref = "2506062281" + (currentQty + labelinfo.Label_Qty).ToString("000000#");
                                    break;
                                case "Case":
                                    Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "01" +
                                                (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                                case "SBUX":
                                    Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "01" +
                                                (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                                case "KBA":
                                    Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "03" +
                                                (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                                case "CC":
                                     Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "04" +
                                                (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                                case "Shipping":
                                    Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "05" +
                                               (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                                default:
                                    Label_Ref = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + "01" +
                                                (currentQty + labelinfo.Label_Qty).ToString("00000#");
                                    break;
                            }
                        }
                    }

                    if (labelinfo.Label_Type == "Shipping" &&
                        !string.IsNullOrWhiteSpace(labelinfo.SalesOrder))
                    {
                        // Save the Sales Order instead of the Work Order
                        labelinfo.Work_Order = labelinfo.SalesOrder;

                        // Shipping labels don't need product information
                        labelinfo.intid = 0;
                        labelinfo.Product = "";
                        labelinfo.Description = "";

                        // Save the shipping address in the Comment field
                        labelinfo.Comment = Request.Form["ShipAddress"];
                    }
                    // Insert the label record
                    string query = @"INSERT INTO label_Count
                                    (Work_Order, Label_Type, intid, Product, Description, Label_Qty, Build_Qty, Label_No, Comment, Created_by) 
                                    VALUES (@Work_Order, @Label_Type, @intid, @Product, @Description, @Label_Qty, @Build_Qty, @Label_No,  @Comment, @Created_by)
                                    RETURNING id";

                    
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Work_Order", labelinfo.Work_Order);
                        command.Parameters.AddWithValue("@Label_Type", labelinfo.Label_Type);
                        command.Parameters.AddWithValue("@intid", labelinfo.intid);
                        command.Parameters.AddWithValue("@Product", labelinfo.Product);
                        command.Parameters.AddWithValue("@Description", labelinfo.Description);
                        command.Parameters.AddWithValue("@Label_Qty", labelinfo.Label_Qty);
                        command.Parameters.AddWithValue("@Build_Qty", labelinfo.Build_Qty);
                        command.Parameters.AddWithValue("@Label_No", Label_Ref);
                        command.Parameters.AddWithValue("@Comment", labelinfo.Comment ?? "");
                        command.Parameters.AddWithValue("@Created_by", User.Identity?.Name ?? "Unknown");

                        insertedId = (int)command.ExecuteScalar();
                    }


                    connection.Close();
                }

                // ✅ Redirect to Print page with intid as query string
                return Redirect($"/Print?id={labelinfo.intid}&intid={insertedId}");
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return RedirectToPage("/Index");
            }
        }


    }
    public class LabelInfo
    {
        public int id = 0;
        public int intid { get; set; }
        public string Create_Date{ get; set; }
        public string Best_Before { get; set; }
        public string Used_By { get; set; }
        public string Expiry { get; set; }
        public string Work_Order { get; set; }
        public string Label_Type { get; set; }
        public string Product { get; set; }
        public string Description { get; set; }
        public string Customer { get; set; }
        public int Label_Qty { get; set; }
        public int Build_Qty { get; set; }
        public string Label_No { get; set; }
        public string PalletCount { get; set; }
        public string CaseCount { get; set; }
        public string KbaCount { get; set; }
        public string CCCount { get; set; }
        public string ShippingCount { get; set; }
        public string SBUXCount { get; set; }
        public string Created_by { get; set; }
        public string Ref_No { get; set; }
        public string Comment { get; set; }

        public string SalesOrder { get; set; }
        public int SalesOrderInternalId { get; set; }
        public string ShipAddress { get; set; }

    }

}

