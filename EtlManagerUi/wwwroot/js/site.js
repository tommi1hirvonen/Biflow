function drawDependencyGraph(workers_, dependencies_) {

    var workers = JSON.parse(workers_);
    var dependencies = JSON.parse(dependencies_);

    // Set up zoom support
    var svg = d3.select("#dependency_graph"),
        inner = svg.select("g"),
        zoom = d3.zoom().on("zoom", function () {
            inner.attr("transform", d3.event.transform);
        });
    svg.call(zoom);

    var render = new dagreD3.render();

    // Left-to-right layout
    var g = new dagreD3.graphlib.Graph();
    g.setGraph({
        nodesep: 70,
        ranksep: 50,
        rankdir: "LR",
        marginx: 20,
        marginy: 20
    });

    for (var id in workers) {
        var worker = workers[id];
        g.setNode(worker.Id, {
            label: worker.Name,
            rx: 20,
            ry: 20
        });
        g.node(worker.Id).id = worker.Id; // Set the id of the node element. Used for onclick purposes.
    }

    for (var id in dependencies) {
        var dep = dependencies[id];
        var className = dep.StrictDependency ? 'strict' : 'non-strict';
        g.setEdge(dep.DependantOnStepId, dep.StepId, { curve: d3.curveBasis, class: className });
    }

    inner.call(render, g);

    // Zoom and scale to fit
    var graphWidth = g.graph().width + 80;
    var graphHeight = g.graph().height + 100;

    var width = parseInt(svg.style("width").replace(/px/, ""));
    var height = parseInt(svg.style("height").replace(/px/, ""));

    var zoomScale = Math.min(width / graphWidth, height / graphHeight);
    var translateX = (width / 2) - ((graphWidth * zoomScale) / 2)
    var translateY = (height / 2) - ((graphHeight * zoomScale) / 2);

    svg.call(zoom.transform, d3.zoomIdentity.translate(translateX, translateY).scale(zoomScale));
}

function drawDurationGraph(datasets_) {

    var datasets1 = JSON.parse(datasets_);

    var ctx1 = document.getElementById('myChart1')

    var myChart1 = new Chart(ctx1, {
        type: 'line',
        data: {
            datasets: datasets1
        },
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    },
                    scaleLabel: {
                        display: true,
                        labelString: 'min'
                    }
                }],
                xAxes: [{
                    type: 'time'
                }]
            }
        }
    })
}

function drawNoOfExecutionsGraph(datasets_) {

    var datasets3 = JSON.parse(datasets_);

    var ctx3 = document.getElementById('myChart3')

    var myChart3 = new Chart(ctx3, {
        type: 'line',
        data: {
            datasets: datasets3
        },
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true,
                        stepSize: 1
                    }
                }],
                xAxes: [{
                    type: 'time'
                }]
            }
        }
    })

}

function drawSuccessRateGraph(labels_, data_, colors_) {

    var labels1 = labels_.split(",");
    var data1 = data_.split(",");
    var colors1 = colors_.split(",");

    // Job success rates
    var ctx2 = document.getElementById('myChart2')
    var myChart2 = new Chart(ctx2, {
        type: 'horizontalBar',
        data: {
            labels: labels1,
            datasets: [{
                data: data1,
                lineTension: 0,
                backgroundColor: colors1
            }]
        },
        options: {
            scales: {
                xAxes: [{
                    ticks: {
                        beginAtZero: true,
                        max: 100,
                        min: 0,
                        stepSize: 10,
                        callback: function (value, index, values) {
                            return value + '%';
                        }
                    }
                }]
            },
            legend: {
                display: false
            }
        }
    })


    

}