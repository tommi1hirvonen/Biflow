using Biflow.Core.Entities;
using Quartz;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Attributes.Validation;

public class CronExpressionAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var schedule = (Schedule)validationContext.ObjectInstance;
        return CronExpression.IsValidExpression(schedule.CronExpression)
            ? ValidationResult.Success!
            : new ValidationResult("Not a valid Cron expression");
    }
}
