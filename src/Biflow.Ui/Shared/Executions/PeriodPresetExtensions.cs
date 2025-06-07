namespace Biflow.Ui.Shared.Executions;

public static class PeriodPresetExtensions
{
    public static string? GetPresetText(this PeriodPreset preset) => preset switch
    {
        PeriodPreset.Week => "Last week",
        PeriodPreset.Month => "Last month",
        PeriodPreset.ThreeMonths => "Last 3 months",
        PeriodPreset.SixMonths => "Last 6 months",
        PeriodPreset.TwelveMonths => "Last 12 months",
        _ => null
    };
    
    public static (DateTime From, DateTime To) GetPresetRange(this PeriodPreset preset)
    {
        return preset switch
        {
            PeriodPreset.Week => TimeSpan.FromDays(7).GetPresetLast(),
            PeriodPreset.Month => TimeSpan.FromDays(30).GetPresetLast(),
            PeriodPreset.ThreeMonths => TimeSpan.FromDays(30 * 3).GetPresetLast(),
            PeriodPreset.SixMonths => TimeSpan.FromDays(30 * 6).GetPresetLast(),
            PeriodPreset.TwelveMonths => TimeSpan.FromDays(360).GetPresetLast(),
            _ => throw new ArgumentOutOfRangeException($"Unhandled {nameof(PeriodPreset)} value: {preset}")
        };
    }
    
    private static (DateTime From, DateTime To) GetPresetLast(this TimeSpan timeSpan)
    {
        var to = DateTime.Today.AddDays(1);
        var from = DateTime.Today.Add(-timeSpan);
        return (from, to);
    }
}