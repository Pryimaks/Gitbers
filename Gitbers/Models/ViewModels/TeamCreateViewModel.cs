using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models.ViewModels
{
    public class TeamCreateViewModel
    {
        [Required(ErrorMessage = "Вкажіть назву команди.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? GitHubOwner { get; set; }

        public string? GitHubRepository { get; set; }
    }
}