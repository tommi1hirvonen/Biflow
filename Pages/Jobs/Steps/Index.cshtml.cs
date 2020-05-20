using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(IConfiguration configuration, ExecutorManager.Data.ExecutorManagerContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public IList<Step> Steps { get;set; }

        public Job Job { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
            Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ToList();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Job = await _context.Jobs.FindAsync(id);

            if (Job == null)
            {
                return NotFound();
            }

            SqlConnection sqlConnection = new SqlConnection(_configuration.GetConnectionString("ExecutorManagerContext"));
            SqlCommand sqlCommand = new SqlCommand(
                "DECLARE @execution_id BIGINT\n" +

                "EXEC[SSISDB].[catalog].[create_execution]\n" +
                    "@package_name = @PackageName,\n" +
                    "@execution_id = @execution_id OUTPUT,\n" +
                    "@folder_name = @FolderName,\n" +
                    "@project_name = @ProjectName,\n" +
                    "@use32bitruntime = 0,\n" +
                    "@reference_id = NULL\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 50,\n" +
                    "@parameter_name = N'LOGGING_LEVEL',\n" +
                    "@parameter_value = 1\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 50,\n" +
                    "@parameter_name = N'SYNCHRONIZED',\n" +
                    "@parameter_value = 0\n" +

                "EXEC[SSISDB].[catalog].[set_execution_parameter_value]\n" +
                    "@execution_id,\n" +
                    "@object_type = 30,\n" +
                    "@parameter_name = N'JobId',\n" +
                    "@parameter_value = @JobId\n" +

                "EXEC[SSISDB].[catalog].[start_execution]\n" +
                    "@execution_id"

                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@PackageName", "MasterExecutor.dtsx");
            sqlCommand.Parameters.AddWithValue("@FolderName", "Executor");
            sqlCommand.Parameters.AddWithValue("@ProjectName", "Executor");
            sqlCommand.Parameters.AddWithValue("@JobId", Job.JobId.ToString());

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
            await sqlConnection.CloseAsync();

            return RedirectToPage("../../Executions/Index");
        }
    }
}
