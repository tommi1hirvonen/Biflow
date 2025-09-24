using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class HttpStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<HttpStepExecutionParameter>,
    IHasStepExecutionAttempts<HttpStepExecutionAttempt>
{
    public HttpStepExecution(string stepName) : base(stepName, StepType.Http)
    {
    }

    public HttpStepExecution(HttpStep step, Execution execution) : base(step, execution)
    {
        TimeoutMinutes = step.TimeoutMinutes;
        Url = step.Url;
        Method = step.Method;
        Body = step.Body;
        Headers = step.Headers.ToArray();
        DisableAsyncPattern = step.DisableAsyncPattern;

        StepExecutionParameters = step.StepParameters
            .Select(p => new HttpStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new HttpStepExecutionAttempt(this));
    }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }
    
    [MaxLength(2048)]
    public string Url { get; init; } = "";
    
    public HttpStepMethod Method { get; init; } = HttpStepMethod.Get;
    
    public string? Body { get; init; }
    
    public HttpHeader[] Headers { get; init; } = [];
    
    /// <summary>
    /// Option to disable invoking HTTP GET on location given in response header of a HTTP 202 Response.
    /// If set true, it stops invoking HTTP GET on http location given in response header.
    /// If set false then continues to invoke HTTP GET call on location given in http response headers.
    /// </summary>
    public bool DisableAsyncPattern { get; init; }

    public IEnumerable<HttpStepExecutionParameter> StepExecutionParameters { get; } = new List<HttpStepExecutionParameter>();

    public override HttpStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new HttpStepExecutionAttempt((HttpStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }
}
