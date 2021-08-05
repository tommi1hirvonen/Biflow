using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public record DatasetStepExecution : StepExecution
    {
        public DatasetStepExecution(string stepName, string datasetGroupId, string datasetId) : base(stepName)
        {
            DatasetGroupId = datasetGroupId;
            DatasetId = datasetId;
        }

        [Display(Name = "App registration id")]
        public Guid AppRegistrationId { get; set; }
        
        [Display(Name = "Group id")]
        public string DatasetGroupId { get; set; }

        [Display(Name = "Dataset id")]
        public string DatasetId { get; set; }
    }
}
