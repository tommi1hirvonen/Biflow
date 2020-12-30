using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi
{
    public class MethodInvokeHelper
    {
        private readonly Action<string> action;

        public MethodInvokeHelper(Action<string> action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void HelperInvokeCaller(string text)
        {
            action.Invoke(text);
        }
    }
}
