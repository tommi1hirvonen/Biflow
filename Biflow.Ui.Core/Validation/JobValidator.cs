using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class JobValidator : AbstractValidator<Job>
{
    public JobValidator()
    {
        RuleFor(job => job.JobParameters)
            .Must(p => p is null || p.DistinctBy(p => p.DisplayName).Count() == p.Count)
            .WithMessage("Parameter names must be unique");
        RuleFor(job => job.JobConcurrencies)
            .Must(jc => jc is null || jc.DistinctBy(c => c.StepType).Count() == jc.Count)
            .WithMessage("Job concurrency step types must be unique");
    }
}