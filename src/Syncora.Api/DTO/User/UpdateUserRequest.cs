using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.User
{
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        [MaxLength(50)]
        public string? Timezone { get; set; }
    }
}
