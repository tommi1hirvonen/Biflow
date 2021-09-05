using Microsoft.AspNetCore.Components.Forms;

namespace EtlManagerUi
{
    public class InputSelectBoolean : InputSelect<bool>
    {
        protected override bool TryParseValueFromString(string? value, out bool result, out string validationErrorMessage)
        {
            if (bool.TryParse(value, out var resultBool))
            {
                result = resultBool;
                validationErrorMessage = string.Empty;
                return true;
            }
            else
            {
                result = default;
                validationErrorMessage = "The chosen value is not a valid boolean.";
                return false;
            }
        }
    }
}
