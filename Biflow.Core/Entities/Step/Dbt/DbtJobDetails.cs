using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class DbtJobDetails
{
    [Required]
    public required long Id { get; init; }

    public required string? Name
    {
        get;
        init => field = value?[..int.Min(value.Length, 500)];
    }

    public required long EnvironmentId { get; init; }

    public required string? EnvironmentName
    {
        get;
        init => field = value?[..int.Min(value.Length, 500)];
    }

    public required long ProjectId { get; init; }

    public required string? ProjectName
    {
        get;
        init => field = value?[..int.Min(value.Length, 500)];
    }
}