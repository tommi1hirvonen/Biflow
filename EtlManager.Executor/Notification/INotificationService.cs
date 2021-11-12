using EtlManager.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManager.Executor;

public interface INotificationService
{
    public Task SendCompletionNotification(Execution execution);

    public Task SendLongRunningExecutionNotification(Execution execution);
}
