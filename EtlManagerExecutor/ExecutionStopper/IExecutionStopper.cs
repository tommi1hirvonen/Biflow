using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor;

interface IExecutionStopper
{
    public Task<bool> RunAsync(string executionId, string? username, string? stepId);
}
