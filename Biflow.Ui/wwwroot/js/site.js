function drawDependencyGraph(steps_, dependencies_) {

    var steps = JSON.parse(steps_);
    var dependencies = JSON.parse(dependencies_);

    var container = document.getElementById('g_dependency_graph');
    // Clear all other previous content.
    // This way we can all previously set listeners as well
    // => no duplicate function calls back to.NET.
    container.innerHTML = '';

    // Set up zoom support
    var svg = d3.select("#svg_dependency_graph"),
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

    for (var id in steps) {
        var step = steps[id];
        var className = step.ClassName;
        g.setNode(step.Id, {
            label: step.Name,
            rx: 20,
            ry: 20,
            class: className
        });
        g.node(step.Id).id = step.Id; // Set the id of the node element. Used for onclick purposes.
    }

    for (var id in dependencies) {
        var dep = dependencies[id];
        var className = dep.ClassName;
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

    // Set event listeners to pass the clicked node's id back to Blazor.
    // Set onClick listeners only for internal steps (steps belonging to current job).
    var onClickElements = document.getElementsByClassName("node internal");
    // Enable tooltips for all steps (steps belonging to other jobs as well).
    var tooltipElements = document.getElementsByClassName("node");

    var myFunction = function (event) {
        var dropdownId = `${this.id}_dropdown`;
        var dropdown = document.getElementById(dropdownId);
        var menu = dropdown.querySelector('.dropdown-menu');
        menu.classList.add('show');
        // Place the dropdown to the location of the mouse.
        var bodyRect = document.body.getBoundingClientRect();
        // Check whether the dropdown menu would overflow over the right side of the window.
        var tempX = event.pageX + menu.clientWidth <= bodyRect.width ? event.pageX : event.pageX - menu.clientWidth;
        // Check whether the dropdown menu would overflow over the bottom of the window.
        var tempY = event.pageY + bodyRect.top + menu.clientHeight <= bodyRect.height ? event.pageY : event.pageY - menu.clientHeight;
        dropdown.style.top = `${tempY}px`;
        dropdown.style.left = `${tempX}px`;

        

        // Close all other dependency graph dropdown menus.
        var menus = document.querySelectorAll('.dependency-graph-dropdown-menu');
        for (var i = 0; i < menus.length; i++) {
            var menu = menus[i];
            if (menu.parentElement.id == dropdownId) continue;
            menu.classList.remove('show');
        }
    };

    for (var i = 0; i < onClickElements.length; i++) {
        var element = onClickElements[i];
        element.addEventListener('click', myFunction, false);
    }
    for (var i = 0; i < tooltipElements.length; i++) {
        var element = tooltipElements[i];
        var title_ = steps.find(s => s.Id == element.id).Tooltip;
        if (typeof title_ == 'string') {
            var tooltip = new bootstrap.Tooltip(element, {
                title: title_
            });
        }
    }
}

function attachDependencyGraphBodyListener() {
    document.body.addEventListener('click', dependencyGraphBodyOnClick);
}

function disposeDependencyGraphBodyListener() {
    document.body.removeEventListener('click', dependencyGraphBodyOnClick);
}

function dependencyGraphBodyOnClick(event) {
    // If the event target was a dependency graph node, skip that specific dropdown menu.
    // Close all other dropdown menus that might be open.
    var dropdownId = `${event.target.__data__}_dropdown`;
    var menus = document.querySelectorAll('.dependency-graph-dropdown-menu');
    for (var i = 0; i < menus.length; i++) {
        var menu = menus[i];
        if (menu.parentElement.id == dropdownId) continue;
        menu.classList.remove('show');
    }
}

function drawDurationGraph(datasets_) {

    var datasets1 = JSON.parse(datasets_);

    var ctx1 = document.getElementById('myChart1')

    // Check if chart already exists from a previous report load and destroy if so.
    var myChart1 = Chart.getChart('myChart1')
    if (myChart1) {
        myChart1.destroy();
    }

    myChart1 = new Chart(ctx1, {
        type: 'line',
        data: {
            datasets: datasets1
        },
        options: {
            scales: {
                y: {
                    position: 'left',
                    title: {
                        display: true,
                        text: 'min'
                    },
                    min: 0
                },
                x: {
                    type: 'time'
                }
            }
        }
    })
}

function drawNoOfExecutionsGraph(datasets_) {

    var datasets3 = JSON.parse(datasets_);

    var ctx3 = document.getElementById('myChart3')

    // Check if chart already exists from a previous report load and destroy if so.
    var myChart3 = Chart.getChart('myChart3')
    if (myChart3) {
        myChart3.destroy();
    }

    myChart3 = new Chart(ctx3, {
        type: 'line',
        data: {
            datasets: datasets3
        },
        options: {
            scales: {
                y: {
                    min: 0,
                    ticks: {
                        stepSize: 1
                    }
                },
                x: {
                    type: 'time'
                }
            }
        }
    })

}

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

function setPropByElement(element, property, value) {
    element[property] = value;
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