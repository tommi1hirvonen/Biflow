using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ExecutorManager.Models
{
    public class Job
    {
        public Guid JobId { get; set; }

        [Display(Name = "Name")]
        public string JobName { get; set; }
        
        [Display(Name = "Created")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDateTime { get; set; }
        
        [Display(Name = "Last Modified")]
        [DataType(DataType.DateTime)]
        public DateTime LastModifiedDateTime { get; set; }

        [Display(Name = "Use Dependency Mode")]
        public bool UseDependencyMode { get; set; }
    }
}
