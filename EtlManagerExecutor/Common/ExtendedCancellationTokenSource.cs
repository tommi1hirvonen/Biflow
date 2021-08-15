using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class ExtendedCancellationTokenSource : CancellationTokenSource
    {
        public string Username { get; private set; } = "timeout";

        public void Cancel(string username)
        {
            Username = username;
            Cancel();
        }
    }
}
