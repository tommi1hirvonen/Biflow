using Biflow.Core.Entities.Scd;
using FluentValidation;

namespace Biflow.Ui.Core.Validation;

public class ScdTableValidator : AbstractValidator<ScdTable>
{
    public ScdTableValidator()
    {
        RuleFor(table => table.NaturalKeyColumns)
            .NotEmpty()
            .WithMessage("At least one natural key column is required");
        
        RuleFor(table => table.NaturalKeyColumns)
            .Must(l => l.DistinctBy(x => x).Count() == l.Count)
            .WithMessage("Natural key columns must be unique");

        RuleFor(table => table.SchemaDriftConfiguration)
            .SetInheritanceValidator(v =>
            {
                v.Add(new DriftEnabledValidator());
                v.Add(new DriftDisabledValidator());
            });
    }
}

file class DriftEnabledValidator : AbstractValidator<SchemaDriftEnabledConfiguration>
{
    public DriftEnabledValidator()
    {
        RuleFor(c => c.ExcludedColumns)
            .Must(l => l.DistinctBy(x => x).Count() == l.Count)
            .WithMessage("Excluded columns must be unique");
    }
}

file class DriftDisabledValidator : AbstractValidator<SchemaDriftDisabledConfiguration>
{
    public DriftDisabledValidator()
    {
        RuleFor(c => c.IncludedColumns)
            .Must(l => l.DistinctBy(x => x).Count() == l.Count)
            .WithMessage("Included columns must be unique");
    }
}