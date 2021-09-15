using EtlManagerDataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public interface INotificationService
    {
        public void SendCompletionNotification(Guid executionId);

        public void SendLongRunningExecutionNotification(Execution execution);
    }
}
