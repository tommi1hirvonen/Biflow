using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

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
