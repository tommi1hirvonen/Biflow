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

namespace EtlManager.Pages.Jobs.Steps
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly Data.EtlManagerContext _context;
        public readonly string WebRootPath;
        private readonly HttpContext httpContext;

        public IndexModel(IConfiguration configuration, Data.EtlManagerContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _context = context;
            WebRootPath = webHostEnvironment.WebRootPath;
            httpContext = httpContextAccessor.HttpContext;
        }

        public IList<Step> Steps { get;set; }

        public Job Job { get; set; }

        [BindProperty]
        public Step NewStep { get; set; }

        [BindProperty]
        public IList<ParameterEdit> Parameters { get; set; } = new List<ParameterEdit>();

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
            Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ThenBy(step => step.StepName).ToList();
            NewStep = new Step { JobId = id };
        }

        
        public async Task<IActionResult> OnPostExecute(Guid id)
        {
            if (id == null)
            {
                return new JsonResult("Id argument was null");
            }

            Job = await _context.Jobs.FindAsync(id);

            if (Job == null)
            {
                return new JsonResult("No job found with provided id");
            }

            string user = httpContext.User?.Identity?.Name;
            Guid executionId_;

            try
            {
                executionId_ = await Utility.StartExecution(_configuration, Job, user);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error starting job: " + ex.Message });
            }

            return new JsonResult(new { success = true, responseText = "Job started successfully", executionId = executionId_ });
        }

        public async Task<IActionResult> OnPostToggleEnabled(Guid? id)
        {
            if (id == null)
            {
                return new JsonResult("Id argument was null");
            }

            Step step = await _context.Steps.FindAsync(id);

            if (step == null)
            {
                return new JsonResult("No step found for id " + id);
            }

            await Utility.ToggleStepEnabled(_configuration, step);

            return new JsonResult("Success");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == null)
            {
                return new JsonResult("Id argument was null");
            }

            Step step = await _context.Steps.FindAsync(id);

            if (step == null)
            {
                return new JsonResult("No step found for id " + id);
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

        public async Task<IActionResult> OnPostCreate()
        {

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
