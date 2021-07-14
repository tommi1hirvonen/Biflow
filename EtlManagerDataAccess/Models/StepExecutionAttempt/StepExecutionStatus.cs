using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public enum StepExecutionStatus
    {
        NotStarted,
        Running,
        Succeeded,
        Failed,
        Stopped,
        Skipped,
        AwaitRetry,
        Duplicate
    }
}
