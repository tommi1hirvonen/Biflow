using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class StepBase
    {
        public string StepId { get; init; }

        public ConfigurationBase ConfigurationBase { get; init; }

        public StepBase(ConfigurationBase configurationBase, string stepId)
        {
            ConfigurationBase = configurationBase;
            StepId = stepId;
        }
    }
}
