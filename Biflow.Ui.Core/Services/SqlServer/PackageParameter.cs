using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public record PackageParameter(ParameterLevel ParameterLevel, string ParameterName, ParameterValueType ParameterType, object? DefaultValue);