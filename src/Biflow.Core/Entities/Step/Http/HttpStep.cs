using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class HttpStep : Step, IHasTimeout, IHasStepParameters<HttpStepParameter>
{
    [JsonConstructor]
    public HttpStep() : base(StepType.Http) { }

    private HttpStep(HttpStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        Url = other.Url;
        Method = other.Method;
        Body = other.Body;
        BodyFormat = other.BodyFormat;
        Headers = other.Headers;
        DisableAsyncPattern = other.DisableAsyncPattern;
        StepParameters = other.StepParameters
            .Select(p => new HttpStepParameter(p, this, targetJob))
            .ToList();
    }
        
    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [MaxLength(2048)]
    [Required]
    public string Url { get; set; } = "";
    
    public HttpStepMethod Method { get; set; } = HttpStepMethod.Post;

    public string? Body
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    public HttpBodyFormat BodyFormat
    {
        get;
        set
        {
            field = value;
            UpdateContentTypeHeader(value);
        }
    } = HttpBodyFormat.Json;
    
    public List<HttpHeader> Headers { get; init; } = [new("Content-Type", "application/json")];
    
    /// <summary>
    /// Option to disable invoking HTTP GET on location given in response header of a HTTP 202 Response.
    /// If set true, it stops invoking HTTP GET on http location given in response header.
    /// If set false then continues to invoke HTTP GET call on location given in http response headers.
    /// </summary>
    public bool DisableAsyncPattern { get; set; }

    [ValidateComplexType]
    [JsonInclude]
    public IList<HttpStepParameter> StepParameters { get; private set; } = new List<HttpStepParameter>();
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Http;

    public override StepExecution ToStepExecution(Execution execution) => new HttpStepExecution(this, execution);

    public override HttpStep Copy(Job? targetJob = null) => new(this, targetJob);

    private void UpdateContentTypeHeader(HttpBodyFormat bodyFormat)
    {
        var header = Headers.FirstOrDefault(h => h.Key == "Content-Type");
        if (header is not null)
        {
            header.Value = bodyFormat switch
            {
                HttpBodyFormat.PlainText => "text/plain",
                HttpBodyFormat.Json => "application/json",
                _ => header.Value
            };
            return;
        }
        string mediaType;
        switch (bodyFormat)
        {
            case HttpBodyFormat.PlainText:
                mediaType = "text/plain";
                break;
            case HttpBodyFormat.Json:
                mediaType = "application/json";
                break;
            default:
                return;
        }
        Headers.Add(new HttpHeader("Content-Type", mediaType));
    }
}
