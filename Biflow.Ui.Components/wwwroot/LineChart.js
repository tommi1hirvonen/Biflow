export function draw(chartId, series, yAxisTitle, yMin, yStepSize, isDarkTheme) {

    var datasets = JSON.parse(series);

    var chartElement = document.getElementById(chartId)

    // Check if chart already exists and destroy if so.
    var chart = Chart.getChart(chartId)
    if (chart) {
        chart.destroy();
    }

    var gridColor = isDarkTheme ? '#393939' : '#e6e6e6';
    var textColor = isDarkTheme ? '#aaaaaa' : '#666666';

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
                        text: yAxisTitle,
                        color: textColor
                    },
                    min: yMin,
                    ticks: {
                        stepSize: yStepSize,
                        color: textColor
                    },
                    grid: {
                        color: gridColor
                    }
                },
                x: {
                    type: 'time',
                    grid: {
                        color: gridColor
                    },
                    ticks: {
                        color: textColor
                    }
                }
            },
            plugins: {
                legend: {
                    labels: {
                        color: textColor
                    }
                }
            }
        }
    })
}