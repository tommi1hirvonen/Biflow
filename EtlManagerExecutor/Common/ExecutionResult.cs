using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public abstract class ExecutionResult
    {
        public string? InfoMessage { get; }

        public ExecutionResult(string? infoMessage)
        {
            InfoMessage = infoMessage;
        }

        public class Success : ExecutionResult
        {
            public Success(string? infoMessage = null) : base(infoMessage)
            {
            }
        }

        public class Failure : ExecutionResult
        {
            public string ErrorMessage { get; } = string.Empty;

            public Failure(string errorMessage, string? infoMessage = null) : base(infoMessage)
            {
                ErrorMessage = errorMessage;
            }
        }
    }
}