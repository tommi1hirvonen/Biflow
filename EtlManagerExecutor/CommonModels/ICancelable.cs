using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface ICancelable
    {
        public Task<bool> CancelAsync();
    }
}
