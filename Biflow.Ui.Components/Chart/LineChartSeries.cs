namespace Biflow.Ui.Components;

public record LineChartSeries(string Label, IEnumerable<TimeSeriesDataPoint> DataPoints, string Color = ChartColors.Indigo, double Tension = 0.3, bool Fill = false);
