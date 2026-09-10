using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class Contact
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Guid ContactUserId { get; set; }

        [MaxLength(100)]
        public string? Nickname { get; set; }

        [MaxLength(50)]
        public string? Timezone { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ContactUserId))]
        public User? ContactUser { get; set; }
    }
}
