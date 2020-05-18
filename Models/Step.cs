using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ExecutorManager.Models
{
    public class Step
    {
        [Key]
        public Guid StepId { get; set; }
        public Guid JobId { get; set; }

        [Display(Name = "Name")]
        public string StepName { get; set; }

        [Display(Name = "Execution Phase")]
        public int ExecutionPhase { get; set; }

        [Display(Name = "Step Type")]
        public string StepType { get; set; }

        [Display(Name = "SQL Statement")]
        public string SqlStatement { get; set; }

        [Display(Name = "Folder Name")]
        public string FolderName { get; set; }

        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }

        [Display(Name = "Package Name")]
        public string PackageName { get; set; }

        [Display(Name = "32 Bit Mode")]
        public bool ExecuteIn32BitMode { get; set; }

        [Display(Name = "Created")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDateTime { get; set; }
        
        [Display(Name = "Last Modified")]
        [DataType(DataType.DateTime)]
        public DateTime LastModifiedDateTime { get; set; }
    }
}
