namespace Biflow.Core.Entities;

public enum MonitoringReason
{
    Duplicate,
    UpstreamDependency,
    DownstreamDependency,
    CommonTarget
}
