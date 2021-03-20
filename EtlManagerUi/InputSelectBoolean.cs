using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi
{
    public class InputSelectBoolean<T> : InputSelect<T>
    {
        protected override bool TryParseValueFromString(string value, out T result,
            out string validationErrorMessage)
        {
            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(value, out var resultBool))
                {
                    result = (T)(object)resultBool;
                    validationErrorMessage = null;
                    return true;
                }
                else
                {
                    result = default;
                    validationErrorMessage = "The chosen value is not a valid boolean.";
                    return false;
                }
            }
            else
            {
                return base.TryParseValueFromString(value, out result, out validationErrorMessage);
            }
        }
    }
}
