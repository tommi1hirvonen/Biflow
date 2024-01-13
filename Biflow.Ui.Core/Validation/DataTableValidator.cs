using FluentValidation;

namespace Biflow.Ui.Core;

public class DataTableValidator : AbstractValidator<MasterDataTable>
{
    public DataTableValidator()
    {
        RuleFor(t => t.Lookups)
            .Must(l => l.DistinctBy(x => x.ColumnName).Count() == l.Count)
            .WithMessage("Lookup columns must be unique");
    }
}
