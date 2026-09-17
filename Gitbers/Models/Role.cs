using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; }
            = new List<User>();
    }
}
