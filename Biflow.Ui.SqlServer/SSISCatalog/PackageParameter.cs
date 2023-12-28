using Biflow.DataAccess.Models;

namespace Biflow.Ui.SqlServer;

public record PackageParameter(ParameterLevel ParameterLevel, string ParameterName, ParameterValueType ParameterType, object? DefaultValue);