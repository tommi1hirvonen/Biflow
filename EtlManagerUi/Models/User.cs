using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class User
    {
        [Key]
        [Required]
        [MaxLength(250)]
        public string Username { get; set; }

        [MaxLength(254)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        public DateTime CreatedDateTime { get; set; }

        [Required]
        public DateTime LastModifiedDateTime { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }
    }
}
