using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class Step
    {
        [Key]
        [Required]
        public Guid StepId { get; set; }

        [Required]
        public Guid JobId { get; set; }

        public Job Job { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Step name")]
        public string StepName { get; set; }

        [Required]
        [Display(Name = "Execution phase")]
        public int ExecutionPhase { get; set; }

        [Required]
        [Display(Name = "Step type")]
        public string StepType { get; set; }

        [Display(Name = "SQL statement")]
        public string SqlStatement { get; set; }

        [MaxLength(50)]
        [Display(Name = "Server name")]
        public string PackageServerName { get; set; }

        [MaxLength(128)]
        [Display(Name = "Folder name")]
        public string PackageFolderName { get; set; }

        [MaxLength(128)]
        [Display(Name = "Project name")]
        public string PackageProjectName { get; set; }

        [MaxLength(260)]
        [Display(Name = "Package name")]
        public string PackageName { get; set; }

        [Required]
        [Display(Name = "32 bit mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Job to execute")]
        public Guid? JobToExecuteId { get; set; }

        [Display(Name = "Synchronized")]
        public bool JobExecuteSynchronized { get; set; }


        public Guid? DataFactoryId { get; set; }
        
        [MaxLength(250)]
        [Display(Name = "Pipeline name")]
        public string PipelineName { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        public DateTime CreatedDateTime { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Last modified")]
        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }

        [Required]
        [Display(Name = "Retry attempts")]
        [Range(0, 10)]
        public int RetryAttempts { get; set; }

        [Required]
        [Display(Name = "Retry interval (min)")]
        [Range(0, 1000)]
        public int RetryIntervalMinutes { get; set; }

        [Display(Name = "Created by")]
        public string CreatedBy { get; set; }

        [Display(Name = "Last modified by")]
        public string LastModifiedBy { get; set; }

        public ICollection<Dependency> Dependencies { get; set; }

        public IList<Parameter> Parameters { get; set; }

        public DataFactory DataFactory { get; set; }
    }
}
