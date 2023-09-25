export function draw(chartId, series, min, max, stepSize, tickSuffix, horizontal, isDarkTheme) {

    var dataset = JSON.parse(series);

    var labels = [];
    var data = [];
    var colors = [];

    for (var id in dataset) {
        var item = dataset[id];
        labels.push(item.label);
        data.push(item.data);
        colors.push(item.color);
    }

    var chartElement = document.getElementById(chartId)

    // Check if chart already exists and destroy if so.
    var chart = Chart.getChart(chartId)
    if (chart) {
        chart.destroy();
    }

    var gridColor = isDarkTheme ? '#393939' : '#e6e6e6';
    var textColor = isDarkTheme ? '#aaaaaa' : '#666666';

    var axis = {
        min: min,
        max: max,
        ticks: {
            stepSize: stepSize,
            callback: function (value, index, values) {
                return tickSuffix != null ? value + tickSuffix : value;
            },
            color: textColor
        },
        grid: {
            color: gridColor
        }
    };
    var otherAxis = {
        ticks: {
            color: textColor
        },
        grid: {
            color: gridColor
        }
    }
    var scale;
    if (horizontal) {
        scale = {
            x: axis,
            y: otherAxis
        }
    } else {
        scale = {
            y: axis,
            x: otherAxis
        }
    }

    chart = new Chart(chartElement, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors
            }]
        },
        options: {
            indexAxis: horizontal ? 'y' : 'x',
            scales: scale,
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    })
}