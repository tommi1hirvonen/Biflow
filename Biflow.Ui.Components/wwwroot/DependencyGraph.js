export function drawDependencyGraph(dotNetObject, graphContainer, svgId, nodesJson, edgesJson, rankdir) {

    const nodes = JSON.parse(nodesJson);
    const edges = JSON.parse(edgesJson);

    // Clear all other previous content.
    // This way we can all previously set listeners as well
    // => no duplicate function calls back to.NET.
    graphContainer.innerHTML = '';

    // Set up zoom support
    const svg = d3.select(`#${svgId}`);
    const inner = svg.select("g");
    const zoom = d3.zoom().on("zoom", function () {
        inner.attr("transform", d3.event.transform);
    });
    svg.call(zoom);

    const render = new dagreD3.render();

    // Left-to-right layout
    const g = new dagreD3.graphlib.Graph();
    g.setGraph({
        nodesep: 70,
        ranksep: 50,
        rankdir: rankdir, // Direction for rank nodes. Can be TB, BT, LR, or RL, where T = top, B = bottom, L = left, and R = right.
        marginx: 20,
        marginy: 20
    });

    for (let id in nodes) {
        const node = nodes[id];
        const className = node.CssClass;
        g.setNode(node.Id, {
            label: node.Name,
            rx: node.Rounded ? 20 : 0,
            ry: node.Rounded ? 20 : 0,
            class: className
        });
        g.node(node.Id).id = node.Id; // Set the id of the node element. Used for onclick purposes.
    }

    for (let id in edges) {
        const edge = edges[id];
        const className = edge.CssClass;
        g.setEdge(edge.DependsOnId, edge.Id, { curve: d3.curveBasis, class: className });
    }

    inner.call(render, g);

    // Zoom and scale to fit
    const graphWidth = g.graph().width + 80;
    const graphHeight = g.graph().height + 100;

    const width = parseInt(svg.style("width").replace(/px/, ""));
    const height = parseInt(svg.style("height").replace(/px/, ""));

    const zoomScale = Math.min(width / graphWidth, height / graphHeight);
    const translateX = (width / 2) - ((graphWidth * zoomScale) / 2)
    const translateY = (height / 2) - ((graphHeight * zoomScale) / 2);

    svg.call(zoom.transform, d3.zoomIdentity.translate(translateX, translateY).scale(zoomScale));

    const nodeOnClick = async function (event) {
        event.preventDefault();
        await dotNetObject.invokeMethodAsync("OnNodeClick", this.id, event.clientX, event.clientY);
    };

    const nodeElements = document.getElementsByClassName("node");

    for (let i = 0; i < nodeElements.length; i++) {
        const element = nodeElements[i];
        const node = nodes.find(s => s.Id == element.id);
        if (node.EnableOnClick) {
            element.addEventListener('click', nodeOnClick, false);
            element.addEventListener('contextmenu', nodeOnClick, false);
        }
        const tooltipText = node.TooltipText;
        if (typeof tooltipText == 'string') {
            const tooltip = new bootstrap.Tooltip(element, {
                title: tooltipText
            });
        }
    }
}