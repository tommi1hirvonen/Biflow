using Biflow.Core.Entities.Scd;
using FluentValidation;

namespace Biflow.Ui.Core.Validation;

public class ScdTableValidator : AbstractValidator<ScdTable>
{
    public ScdTableValidator()
    {
        RuleFor(table => table)
            .Must(table => table.SourceTableSchema != table.TargetTableSchema
                           || table.SourceTableName != table.TargetTableName)
            .WithMessage("The source and target table cannot be the same.");
            
        RuleFor(table => table)
            .Must(table => table.SourceTableSchema != table.StagingTableSchema
                           || table.SourceTableName != table.StagingTableName)
            .WithMessage("The source and staging table cannot be the same.");
        
        RuleFor(table => table)
            .Must(table => table.StagingTableSchema != table.TargetTableSchema
                           || table.StagingTableName != table.TargetTableName)
            .WithMessage("The staging and target table cannot be the same.");
        
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