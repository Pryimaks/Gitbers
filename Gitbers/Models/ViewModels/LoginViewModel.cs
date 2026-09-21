using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введи email.")]
        [EmailAddress(ErrorMessage = "Некоректний email.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Введи пароль.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;
    }
}