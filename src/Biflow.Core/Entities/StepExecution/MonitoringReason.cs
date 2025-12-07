namespace Biflow.Core.Entities;

public enum MonitoringReason
{
    Duplicate,
    UpstreamDependency,
    DownstreamDependency,
    CommonTarget,
    CommonConnection,
    CommonFunctionApp,
    CommonPipelineClient,
    CommonProxy
}
