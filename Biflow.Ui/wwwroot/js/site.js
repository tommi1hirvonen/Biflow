function drawSuccessRateGraph(datasets_) {

    var dataset = JSON.parse(datasets_);

    var labels1 = [];
    var data1 = [];
    var colors1 = [];

    for (var id in dataset) {
        var item = dataset[id];
        labels1.push(item.label);
        data1.push(item.data);
        colors1.push(item.color);
    }

    // Job success rates
    var ctx2 = document.getElementById('myChart2')

    // Check if chart already exists from a previous report load and destroy if so.
    var myChart2 = Chart.getChart('myChart2')
    if (myChart2) {
        myChart2.destroy();
    }

    myChart2 = new Chart(ctx2, {
        type: 'bar',
        data: {
            labels: labels1,
            datasets: [{
                data: data1,
                backgroundColor: colors1
            }]
        },
        options: {
            indexAxis: 'y',
            scales: {
                x: {
                    min: 0,
                    max: 100,
                    ticks: {
                        stepSize: 10,
                        callback: function (value, index, values) {
                            return value + '%';
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    })

}

function highlightCode() {
    document.querySelectorAll('pre code').forEach((el) => {
        hljs.highlightElement(el);
    });
}

async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}