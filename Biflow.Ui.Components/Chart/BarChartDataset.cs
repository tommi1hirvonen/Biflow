namespace Biflow.Ui.Components;

public record BarChartDataset(IEnumerable<BarChartDataPoint> DataPoints, int? Min = null, int? Max = null, int? StepSize = null, string? TickSuffix = null, bool Horizontal = false);