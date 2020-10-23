using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Executions
{
    public class JobDetailsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuthorizationService _authorizationService;
        private readonly HttpContext _httpContext;

        public JobDetailsModel(Data.EtlManagerContext context, IConfiguration configuration, IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _authorizationService = authorizationService;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public Guid ExecutionId { get; set; }

        public bool IsOperator { get; set; } = false;

        public bool Graph = true;
        public IList<StepExecution> Executions { get; set; }

        public JobExecution JobExecution { get; set; }

        public async Task OnGetAsync(Guid id, bool graph = true)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (authorized.Succeeded)
            {
                IsOperator = true;
            }

            ExecutionId = id;
            Executions = await _context.Executions
                .Where(e => e.ExecutionId == id)
                .OrderBy(execution => execution.CreatedDateTime)
                .ThenBy(execution => execution.StartDateTime)
                .ToListAsync();
            JobExecution = await _context.JobExecutions
                .FirstOrDefaultAsync(e => e.ExecutionId == id);

            Graph = graph;

            if (JobExecution == null)
            {
                JobExecution = new JobExecution { ExecutionId = id, JobName = "Waiting for execution to start..." };
                return;
            }

        }

        public async Task<JsonResult> OnPostStopJobExecution(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            string username = _httpContext.User?.Identity?.Name;

            try
            {
                await Utility.StopJobExecution(_configuration, id, username);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error stopping execution: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnPostStopStepExecution(Guid executionId, Guid stepId, int retryAttemptIndex)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (executionId == null || stepId == null)
            {
                return new JsonResult(new { success = false, responseText = "Invalid arguments" });
            }

            string username = _httpContext.User?.Identity?.Name;

            try
            {
                await Utility.StopStepExecution(_configuration, executionId, stepId, retryAttemptIndex, username);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error stopping step: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

    }
}