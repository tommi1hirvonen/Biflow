using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtlManagerUtils;

namespace EtlManagerDataAccess.Models
{
    public class PackageStepExecutionAttempt : StepExecutionAttempt
    {
        public PackageStepExecutionAttempt(string executionStatus) : base(executionStatus)
        {
        }

        public long? PackageOperationId { get; set; }
    }
}
