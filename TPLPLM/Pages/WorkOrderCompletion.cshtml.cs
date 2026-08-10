using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Data.Odbc;
using System.Collections.Generic;
using System;
using NetSuite;

namespace TPLPLM.Pages
{
    public class WorkOrderCompletionModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkOrderCompletionModel> _logger;
        private readonly NetSuiteClient _netSuiteClient;

        // Constructor
        public WorkOrderCompletionModel(NetSuiteClient netSuiteClient, IConfiguration configuration, ILogger<WorkOrderCompletionModel> logger)
        {
            _netSuiteClient = netSuiteClient;
            _configuration = configuration;
            _logger = logger;
        }

        // Properties
        public List<LabelInfo> Productlist { get; set; } = new List<LabelInfo>();

        [BindProperty]
        [Required]
        public string WorkOrder { get; set; }

        [BindProperty]
        [Required]
        public string AssemblyItem { get; set; }

        [BindProperty]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int CompletionQty { get; set; }

        [BindProperty]
        public string Memo { get; set; }

        // Flag for showing modal
        public bool ShowModalOnLoad { get; set; } = true;

        // OnGet
        public void OnGet()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("NetSuiteOdbc");
                string lastMonthDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");

                using (var connection = new OdbcConnection(connectionString))
                {
                    connection.Open();
                    string sql = $@"
                        SELECT DISTINCT 
                            a.tranid, 
                            c.id AS intid, 
                            c.itemid, 
                            c.displayname
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

                    using (var command = new OdbcCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Productlist.Add(new LabelInfo
                            {
                                intid = reader.GetInt32(reader.GetOrdinal("intid")),
                                Ref_No = reader["tranid"].ToString(),
                                Product = reader["itemid"].ToString(),
                                Description = reader["displayname"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch recent work orders.");
            }
        }

        // OnPost
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                OnGet(); // Reload dropdown data if post fails
                return Page();
            }

            try
            {
                await _netSuiteClient.CompleteWorkOrder(WorkOrder, CompletionQty, AssemblyItem, Memo);
                TempData["SuccessMessage"] = "Work order completed successfully.";
                return RedirectToPage(); // Refresh the page with success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete work order.");
                ModelState.AddModelError(string.Empty, "An error occurred while completing the work order.");
                OnGet(); // Reload data again
                return Page();
            }
        }
    }
}
