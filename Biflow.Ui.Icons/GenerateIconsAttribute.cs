using System;

namespace Biflow.Ui.Icons;

[AttributeUsage(AttributeTargets.Class)]
public class GenerateIconsAttribute(params string[] iconsLocationPathSegments) : Attribute
{
    public string[] IconsLocationPathSegments { get; } = iconsLocationPathSegments;
}