using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace EtlManager.Pages.Jobs.JobDetails
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly Data.EtlManagerContext _context;
        private readonly HttpContext httpContext;
        private readonly IAuthorizationService _authorizationService;

        public IndexModel(IConfiguration configuration, Data.EtlManagerContext context, IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService)
        {
            _configuration = configuration;
            _context = context;
            httpContext = httpContextAccessor.HttpContext;
            _authorizationService = authorizationService;
        }

        public IList<Step> Steps { get;set; }

        public Job Job { get; set; }
        
        public IList<Job> Jobs { get; set; }

        public bool Subscribed { get; set; }

        [BindProperty]
        public Step NewStep { get; set; }

        [BindProperty]
        public IList<ParameterEdit> Parameters { get; set; } = new List<ParameterEdit>();

        public async Task OnGetAsync(Guid id)
        {
            Jobs = await _context.Jobs.OrderBy(job => job.JobName).ToListAsync();
            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
            Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ThenBy(step => step.StepName).ToList();
            NewStep = new Step { JobId = id, RetryAttempts = 0, RetryIntervalMinutes = 0 };
        }

        
        public async Task<IActionResult> OnPostExecute(Guid id, string stepIds)
        {
            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            if (stepIds == null)
            {
                return new JsonResult(new { success = false, responseText = "List of step ids was empty" });
            }

            Job = await _context.Jobs.FindAsync(id);

            if (Job == null)
            {
                return new JsonResult(new { success = false, responseText = "No job found with provided id" });
            }

            string user = httpContext.User?.Identity?.Name;
            Guid executionId_;
            List<string> stepList = stepIds.Split(',').ToList();

            try
            {
                executionId_ = await Utility.StartExecution(_configuration, Job, user, stepList);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error starting job: " + ex.Message });
            }

            return new JsonResult(new { success = true, responseText = "Job started successfully", executionId = executionId_ });
        }

        public async Task<IActionResult> OnPostToggleEnabled(Guid? id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            Step step = await _context.Steps.FindAsync(id);

            if (step == null)
            {
                return new JsonResult(new { success = false, responseText = "No step found for id " + id });
            }

            await Utility.ToggleStepEnabled(_configuration, step);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostToggleDependencyMode(Guid? id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            Job job = await _context.Jobs.FindAsync(id);

            if (job == null)
            {
                return new JsonResult(new { success = false, responseText = "No job found for id " + id });
            }

            await Utility.ToggleJobDependencyMode(_configuration, job);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            Step step = await _context.Steps.FindAsync(id);

            if (step == null)
            {
                return new JsonResult(new { success = false, responseText = "No step found for id " + id });
            }

            try
            {
                _context.Steps.Remove(step);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error deleting step: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCopy(Guid stepId, Guid targetJobId, Guid sourceJobId)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            string user = httpContext.User?.Identity?.Name;

            await Utility.StepCopy(_configuration, stepId, targetJobId, user);

            return RedirectToPage("./Index", new { id = sourceJobId });
        }


        public async Task<IActionResult> OnPostCreate()
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (NewStep.StepType == "SSIS")
            {
                NewStep.Parameters = new List<Parameter>();
                foreach (var parameter in Parameters)
                {
                    if (!parameter.IsDeleted)
                    {
                        NewStep.Parameters.Add(parameter);
                    }
                }
            }

            _context.Steps.Add(NewStep);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = NewStep.JobId });
        }

    }
}
