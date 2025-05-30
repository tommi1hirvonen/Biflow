﻿using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(ExistingClusterConfiguration), "ExistingCluster")]
[JsonDerivedType(typeof(NewClusterConfiguration), "NewCluster")]
public abstract class ClusterConfiguration;

public class ExistingClusterConfiguration : ClusterConfiguration
{
    public string ClusterId { get; set; } = "";

    /// <summary>
    /// The cluster name is stored only for audit purposes
    /// so that it can be viewed in the execution logs
    /// without having to navigate to the actual Databricks workspace.
    /// </summary>
    public string? ClusterName { get; set; }
}

public class NewClusterConfiguration : ClusterConfiguration
{
    public string NodeTypeId { get; set; } = "";

    public string? DriverNodeTypeId { get; set; }

    public string RuntimeVersion { get; set; } = "";

    public bool UsePhoton { get; set; }

    public ClusterModeConfiguration ClusterMode { get; set; } = new AutoscaleMultiNodeClusterConfiguration();
}

[JsonDerivedType(typeof(FixedMultiNodeClusterConfiguration), "FixedMultiNode")]
[JsonDerivedType(typeof(AutoscaleMultiNodeClusterConfiguration), "AutoscaleMultiNode")]
[JsonDerivedType(typeof(SingleNodeClusterConfiguration), "SingleNode")]
public abstract class ClusterModeConfiguration;

public class SingleNodeClusterConfiguration : ClusterModeConfiguration;

public class FixedMultiNodeClusterConfiguration : ClusterModeConfiguration
{
    public int NumberOfWorkers { get; set; } = 1;
}

public class AutoscaleMultiNodeClusterConfiguration : ClusterModeConfiguration
{
    public int MinimumWorkers { get; set; } = 1;

    public int MaximumWorkers { get; set; } = 2;
}