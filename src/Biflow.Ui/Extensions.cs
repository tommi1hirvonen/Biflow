namespace Biflow.Ui;

using Biflow.Core.Entities;

public static class Extensions
{
    /// <summary>
    /// Convenience method for toggling on/off <see cref="DynamicParameter.UseExpression"/>.
    /// When the value is set to false,
    /// the <see cref="ParameterValue"/> will be generated with the provided <see cref="ParameterValueType"/>. 
    /// </summary>
    /// <param name="parameter">The parameter for which to toggle <see cref="DynamicParameter.UseExpression"/></param>
    /// <param name="valueType">The type to use for the default value</param>
    public static void ToggleUseExpression(this DynamicParameter parameter, ParameterValueType valueType)
    {
        parameter.UseExpression = !parameter.UseExpression;
        if (!parameter.UseExpression)
        {
            parameter.ParameterValue = ParameterValue.DefaultValue(valueType);
        }
    }
}