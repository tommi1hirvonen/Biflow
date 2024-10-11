using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(QlikAppReloadSettings), "AppReload")]
[JsonDerivedType(typeof(QlikAutomationRunSettings), "AutomationRun")]
public abstract class QlikStepSettings;

public class QlikAppReloadSettings : QlikStepSettings
{
    [Required]
    [MaxLength(36)]
    public string AppId { get; set; } = "";

    public string? AppName { get; set; }
}

public class QlikAutomationRunSettings : QlikStepSettings
{
    [Required]
    [MaxLength(36)]
    public string AutomationId { get; set; } = "";

    public string? AutomationName { get; set; }
}