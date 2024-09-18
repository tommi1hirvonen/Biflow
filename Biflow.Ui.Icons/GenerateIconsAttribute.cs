using System;

namespace Biflow.Ui.Icons;

[AttributeUsage(AttributeTargets.Class)]
public class GenerateIconsAttribute(string? cssClass, params string[] iconsLocationPathSegments) : Attribute
{
    public string? CssClass { get; } = cssClass;

    public string[] IconsLocationPathSegments { get; } = iconsLocationPathSegments;
}