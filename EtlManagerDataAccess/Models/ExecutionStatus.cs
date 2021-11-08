using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models;

public enum ExecutionStatus
{
    NotStarted,
    Running,
    Succeeded,
    Failed,
    Warning,
    Stopped,
    Suspended
}
