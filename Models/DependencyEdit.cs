using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Models
{
    public class DependencyEdit : Dependency
    {
        public bool Enabled { get; set; }
    }
}
