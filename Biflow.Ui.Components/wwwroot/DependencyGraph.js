export function drawDependencyGraph(graphContainer, svgId, nodesJson, edgesJson) {

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
            rx: 20,
            ry: 20,
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

    var nodeOnClick = function (event) {
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

export function attachDependencyGraphBodyListener() {
    document.body.addEventListener('click', dependencyGraphBodyOnClick);
}

export function disposeDependencyGraphBodyListener() {
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