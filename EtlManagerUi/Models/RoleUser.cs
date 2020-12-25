using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class RoleUser
    {
        [Key]
        [Required]
        [MaxLength(250)]
        public string Username { get; set; }

        [MaxLength(254)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public DateTime LastModifiedDateTime { get; set; }

        [Required]
        public string Role { get; set; }

    }
}
