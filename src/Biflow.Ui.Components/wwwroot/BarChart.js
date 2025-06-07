export function draw(chartId, series, min, max, stepSize, tickSuffix, horizontal, isDarkTheme) {

    const dataset = JSON.parse(series);

    const labels = [];
    const data = [];
    const colors = [];

    for (let id in dataset) {
        const item = dataset[id];
        labels.push(item.label);
        data.push(item.data);
        colors.push(item.color);
    }

    const chartElement = document.getElementById(chartId)

    // Check if chart already exists and destroy if so.
    let chart = Chart.getChart(chartId)
    if (chart) {
        chart.destroy();
    }

    const gridColor = isDarkTheme ? '#393939' : '#e6e6e6';
    const textColor = isDarkTheme ? '#aaaaaa' : '#666666';

    const axis = {
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
    const otherAxis = {
        ticks: {
            color: textColor
        },
        grid: {
            color: gridColor
        }
    }
    let scale;
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