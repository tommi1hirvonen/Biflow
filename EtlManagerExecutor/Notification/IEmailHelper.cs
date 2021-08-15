using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public interface IEmailHelper
    {
        public void SendNotification(Guid executionId);
    }
}
