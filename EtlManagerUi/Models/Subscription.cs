using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi.Models
{
    public class Subscription
    {
        [Key]
        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        public Guid JobId { get; set; }

        public Job Job { get; set; }

        [Required]
        [ForeignKey("User")]
        public string Username { get; set; }

        public User User { get; set; }
    }
}
