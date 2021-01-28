using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class JobStep : StepBase
    {
        protected string JobToExecuteId { get; init; }
        protected bool JobExecuteSynchronized { get; init; }

        public JobStep(ConfigurationBase configuration,string stepId, string jobToExecuteId, bool jobExecuteSynchronized) : base(configuration, stepId)
        {
            JobToExecuteId = jobToExecuteId;
            JobExecuteSynchronized = jobExecuteSynchronized;
        }
    }
}
