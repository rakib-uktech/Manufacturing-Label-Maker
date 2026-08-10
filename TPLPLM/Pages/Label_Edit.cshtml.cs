using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Data.Odbc;
namespace TPLPLM.Pages
{
    public class Label_EditModel : PageModel
	{

        public String errorMessage = "";
        public String successMessage = "";
        public LabelInfo labelinfo = new LabelInfo();
        private readonly IConfiguration _configuration;
        public string DefaultConnection { get; private set; }
        public string _connectionString { get; private set; }
        public Label_EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<LabelInfo> LabelTypelist = new List<LabelInfo>();
        public List<LabelInfo> Productlist = new List<LabelInfo>();
        public List<LabelInfo> Descriptionlist = new List<LabelInfo>();
      


        public void OnGet()
        {
            string id = Request.Query["id"];
            int label_id = Int32.Parse(id);

            try
            {

                string _connectionString = _configuration.GetConnectionString("NetSuiteOdbc");

                using (OdbcConnection NetSuiteConnection = new OdbcConnection(_connectionString))
                {
                    NetSuiteConnection.Open();

                    Productlist.Clear();
                    string lastMonthDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");

                    string sql = $@"
                        SELECT a.tranid, 
                               MIN(c.id) AS intid, 
                               MIN(c.itemid) AS itemid, 
                               MIN(c.displayname) AS displayname
                        FROM transaction a
                        INNER JOIN transactionLine b ON a.id = b.createdfrom
                        INNER JOIN item c ON b.item = c.id
                        WHERE RTRIM(a.recordtype) = 'workorder' 
                        AND c.itemtype='Assembly'
                        AND b.id=0
                        AND a.createddate >= {{d '{lastMonthDate}'}}
                        GROUP BY a.tranid,
                                itemid, 
                                displayname
                        ORDER BY a.tranid DESC";

                    using (OdbcCommand command = new OdbcCommand(sql, NetSuiteConnection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var labelinfo = new LabelInfo
                                {
                                    intid = reader.GetInt32(reader.GetOrdinal("intid")),
                                    Ref_No = reader["tranid"].ToString(),
                                    Product = !reader.IsDBNull(reader.GetOrdinal("itemid")) ? reader["itemid"].ToString() : "Unknown",
                                    Description = !reader.IsDBNull(reader.GetOrdinal("displayname")) ? reader["displayname"].ToString() : "No Description"
                                };

                                Productlist.Add(labelinfo);
                            }
                            reader.Close();
                        }
                    }
                    string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

                    using (NpgsqlConnection connection = new NpgsqlConnection(DefaultConnection))
                    {
                        connection.Open();

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


                        sql = "SELECT * FROM label_Count WHERE id=@id";

                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", label_id);
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    labelinfo.id = reader.GetInt32("id");
                                    labelinfo.Create_Date = reader.GetDateTime("Create_Date").ToString("dd/MM/yyyy");
                                    labelinfo.Label_Type = reader.GetString("Label_Type");
                                    labelinfo.Product = reader.GetString("Product");
                                    labelinfo.Description = reader.GetString("Description");
                                    labelinfo.Label_Qty = reader.GetInt32("Label_Qty");
                                    labelinfo.Build_Qty = reader.GetInt32(reader.GetOrdinal("build_qty"));
                                    labelinfo.Label_No = reader.GetString("Label_No");
                                    // ✅ ADD THIS
                                    labelinfo.Comment = reader.IsDBNull(reader.GetOrdinal("comment"))
                                        ? ""
                                        : reader.GetString("comment");
                                }
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

      
		public void OnPost()
		{
            string id = Request.Query["id"];
            int label_id = Int32.Parse(id);
            labelinfo.Label_Type = Request.Form["LabelType"];
            labelinfo.Product = Request.Form["Item_No"];
            labelinfo.Description = Request.Form["Item_Name"];
            labelinfo.Label_Qty = Convert.ToInt32(Request.Form["Label_Qty"]);
            labelinfo.Comment = Request.Form["Comment"];

            try
            {
                string DefaultConnection = _configuration.GetConnectionString("DefaultConnection");

                using (NpgsqlConnection connection = new NpgsqlConnection(DefaultConnection))
				{
					connection.Open();
                    string sql = @"UPDATE label_Count 
                       SET Label_Type=@Label_Type,
                           Product=@Product,
                           Description=@Description,
                           Label_Qty=@Label_Qty,
                           Comment=@Comment,
                           Created_by=@Created_by
                       WHERE id=@id";


                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
					{
                        command.Parameters.AddWithValue("id", label_id);
                        command.Parameters.AddWithValue("Label_Type", labelinfo.Label_Type);
						command.Parameters.AddWithValue("Product", labelinfo.Product);
						command.Parameters.AddWithValue("Description", labelinfo.Description);
						command.Parameters.AddWithValue("Label_Qty", labelinfo.Label_Qty);
                        command.Parameters.AddWithValue("Comment",string.IsNullOrWhiteSpace(labelinfo.Comment)
                        ? (object)DBNull.Value: labelinfo.Comment);

                        command.Parameters.AddWithValue("Created_by", User.Identity?.Name);
						command.ExecuteNonQuery();
					}
				}

			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return;

			}
			labelinfo.id = 0;
			labelinfo.Create_Date = "";
			labelinfo.Label_Type = "";
			labelinfo.Product = "";
			labelinfo.Description = "";
			labelinfo.Label_Qty =0;
			labelinfo.Label_No = "";
            labelinfo.Created_by = "";

             
        successMessage = "New Label Addedd Sucessfully!";

			Response.Redirect("/Index");

		}

	}
}
