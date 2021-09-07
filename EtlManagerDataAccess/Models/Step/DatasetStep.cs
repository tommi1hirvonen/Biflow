using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class DatasetStep : Step
    {
        public DatasetStep() : base(StepType.Dataset) { }

        [Required]
        public Guid? AppRegistrationId { get; set; }

        [Display(Name = "Group id")]
        [MaxLength(36)]
        [MinLength(36)]
        [Required]
        public string? DatasetGroupId { get; set; }

        [Display(Name = "Dataset id")]
        [MaxLength(36)]
        [MinLength(36)]
        [Required]
        public string? DatasetId { get; set; }

        public AppRegistration? AppRegistration { get; set; }
    }
}
