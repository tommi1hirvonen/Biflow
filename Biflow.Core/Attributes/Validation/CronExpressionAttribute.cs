using Biflow.Core.Entities;
using Quartz;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Attributes.Validation;

public class CronExpressionAttribute : ValidationAttribute
{
    public CronExpressionAttribute()
    {
    }

    public static string GetErrorMessage() => "Not a valid Cron expression";

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var schedule = (Schedule)validationContext.ObjectInstance;
        if (!CronExpression.IsValidExpression(schedule?.CronExpression ?? string.Empty))
        {
            return new ValidationResult(GetErrorMessage());
        }
        return ValidationResult.Success!;
    }
}
