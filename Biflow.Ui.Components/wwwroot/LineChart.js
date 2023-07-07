export function draw(chartId, series, yAxisTitle, yMin, yStepSize) {

    var datasets = JSON.parse(series);

    var chartElement = document.getElementById(chartId)

    // Check if chart already exists and destroy if so.
    var chart = Chart.getChart(chartId)
    if (chart) {
        chart.destroy();
    }

    chart = new Chart(chartElement, {
        type: 'line',
        data: {
            datasets: datasets
        },
        options: {
            scales: {
                y: {
                    title: {
                        display: yAxisTitle != null,
                        text: yAxisTitle
                    },
                    min: yMin,
                    ticks: {
                        stepSize: yStepSize
                    }
                },
                x: {
                    type: 'time'
                }
            }
        }
    })
}