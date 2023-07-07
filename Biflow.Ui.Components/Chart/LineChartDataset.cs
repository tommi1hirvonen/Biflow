namespace Biflow.Ui.Components;

public record LineChartDataset(IEnumerable<LineChartSeries> Series, string? YAxisTitle = null, int? YMin = null, int? YStepSize = null);
