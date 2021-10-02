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
        public Task SendCompletionNotification(Execution execution);

        public Task SendLongRunningExecutionNotification(Execution execution);
    }
}
