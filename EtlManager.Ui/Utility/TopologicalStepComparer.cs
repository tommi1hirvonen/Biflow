using System;
using System.Collections.Generic;
using System.Linq;
using EtlManager.DataAccess.Models;

namespace EtlManager.Ui;

public class TopologicalStepComparer : IComparer<Step>
{
    private Guid[] TopologicalList { get; init; }

    public TopologicalStepComparer(IEnumerable<Step> steps)
    {
        TopologicalList = InTopologicalOrder(steps).Select(step => step.StepId).ToArray();
    }

    public int Compare(Step? x, Step? y)
    {
        int xPos = Array.IndexOf(TopologicalList, x?.StepId ?? Guid.Empty);
        int yPos = Array.IndexOf(TopologicalList, y?.StepId ?? Guid.Empty);
        return xPos.CompareTo(yPos);
    }

    private enum VisitState { NotVisited, Visiting, Visited }

    private static bool DepthFirstSearch(Step current, IEnumerable<Step> steps, Dictionary<Step, VisitState> visited, Stack<Step> stack)
    {
        var state = VisitState.NotVisited;
        visited.TryGetValue(current, out state);
        if (state != VisitState.NotVisited)
        {
            return state == VisitState.Visited; // returns false if already visiting => cycles
        }
        visited[current] = VisitState.Visiting;
        var dependencies = steps.Where(step => current.Dependencies.Any(dep => dep.DependantOnStepId == step.StepId)).ToList();
        var result = dependencies.Aggregate(true, (accumulator, step) => accumulator && DepthFirstSearch(step, steps, visited, stack));
        visited[current] = VisitState.Visited;
        stack.Push(current);
        return result;
    }

    /// <summary>
    /// Orders an IEnumerable of steps in a topological order based on their dependencies.
    /// </summary>
    /// <param name="steps">IEnumerable of steps to be ordered. The navigation property Dependencies should be included.</param>
    /// <returns>IEnumerable of steps in topological order. InvalidOperationException will be thrown if cyclic dependencies are detected.</returns>
    private static IEnumerable<Step> InTopologicalOrder(IEnumerable<Step> steps)
    {
        var stack = new Stack<Step>();
        var visited = new Dictionary<Step, VisitState>();
        foreach (var step in steps.OrderBy(s => s.Dependencies.Count).ThenBy(s => s.StepName))
        {
            if (!DepthFirstSearch(step, steps, visited, stack))
            {
                throw new InvalidOperationException("Cyclic dependencies detected");
            }
        }
        return stack.Reverse();
    }
}
