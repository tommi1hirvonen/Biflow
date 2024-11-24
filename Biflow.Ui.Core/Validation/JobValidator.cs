using FluentValidation;

namespace Biflow.Ui.Core;

public class JobValidator : AsyncAbstractValidator<Job>
{
    public JobValidator()
    {
        RuleFor(job => job.JobParameters)
            .Must(p1 => p1 is null || p1.DistinctBy(p2 => p2.DisplayName).Count() == p1.Count)
            .WithMessage("Parameter names must be unique");
        RuleFor(job => job.JobConcurrencies)
            .Must(jc => jc is null || jc.DistinctBy(c => c.StepType).Count() == jc.Count)
            .WithMessage("Job concurrency step types must be unique");
        RuleForEach(job => job.JobParameters)
            .CustomAsync(async (param, context, _) =>
            {
                try
                {
                    await param.EvaluateAsync();
                }
                catch
                {
                    context.AddFailure($"Error evaluating parameter '{param.DisplayName}'");
                }
            });
    }
}