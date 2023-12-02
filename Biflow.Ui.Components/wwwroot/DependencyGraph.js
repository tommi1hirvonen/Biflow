export function drawDependencyGraph(dotNetObject, graphContainer, svgId, nodesJson, edgesJson) {

    var nodes = JSON.parse(nodesJson);
    var edges = JSON.parse(edgesJson);

    // Clear all other previous content.
    // This way we can all previously set listeners as well
    // => no duplicate function calls back to.NET.
    graphContainer.innerHTML = '';

    // Set up zoom support
    var svg = d3.select(`#${svgId}`);
    var inner = svg.select("g");
    var zoom = d3.zoom().on("zoom", function () {
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

    for (var id in nodes) {
        var node = nodes[id];
        var className = node.CssClass;
        g.setNode(node.Id, {
            label: node.Name,
            rx: node.Rounded ? 20 : 0,
            ry: node.Rounded ? 20 : 0,
            class: className
        });
        g.node(node.Id).id = node.Id; // Set the id of the node element. Used for onclick purposes.
    }

    for (var id in edges) {
        var edge = edges[id];
        var className = edge.CssClass;
        g.setEdge(edge.DependsOnId, edge.Id, { curve: d3.curveBasis, class: className });
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

    var nodeOnClick = async function (event) {
        await dotNetObject.invokeMethodAsync("OnNodeClick", this.id, event.clientX, event.clientY);
    };

    var nodeElements = document.getElementsByClassName("node");

    for (var i = 0; i < nodeElements.length; i++) {
        var element = nodeElements[i];
        var node = nodes.find(s => s.Id == element.id);
        if (node.EnableOnClick) {
            element.addEventListener('click', nodeOnClick, false);
        }
        var tooltipText = node.TooltipText;
        if (typeof tooltipText == 'string') {
            var tooltip = new bootstrap.Tooltip(element, {
                title: tooltipText
            });
        }
    }
}