using Biflow.Core.Entities;

namespace Biflow.Ui.SqlMetadataExtensions;

public record PackageParameter(ParameterLevel ParameterLevel, string ParameterName, ParameterValue Value);