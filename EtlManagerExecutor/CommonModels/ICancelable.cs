using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface ICancelable
    {
        public int RetryAttemptCounter { get; set; }
        public Task<bool> CancelAsync();
    }
}
