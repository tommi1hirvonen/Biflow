using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Models;
using Microsoft.Extensions.Configuration;


namespace EtlManager.Pages.Jobs.Steps
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly Data.EtlManagerContext _context;

        public IndexModel(IConfiguration configuration, Data.EtlManagerContext context)
        {
            _configuration = configuration;
            _context = context;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="redirect">true if the user should be redirected to Executions/Index, false if not</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostExecuteAsync(Guid id, bool redirect)
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

            await Utility.StartExecution(_configuration, Job);

            if (redirect)
            {
                return RedirectToPage("../../Executions/Jobs");
            }
            else
            {
                // Load again for post
                Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
                Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ThenBy(step => step.StepName).ToList();
                return Page();
            }
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
            if (id == null) return NotFound();

            Step step = await _context.Steps.FindAsync(id);

            if (step == null) return NotFound();

            _context.Steps.Remove(step);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = step.JobId });
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
