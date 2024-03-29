using Biflow.Core.Entities;

namespace Biflow.Ui.SqlServer;

public record PackageParameter(ParameterLevel ParameterLevel, string ParameterName, ParameterValue Value);