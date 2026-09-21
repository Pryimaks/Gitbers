using System.ComponentModel.DataAnnotations;

namespace Gitbers.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Введи email.")]
        [EmailAddress(ErrorMessage = "Некоректний email.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Введи ім'я.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "Ім'я повинно містити від 2 до 100 символів.")]
        [Display(Name = "Ім'я")]
        public string FullName { get; set; } = string.Empty;


        [Phone(ErrorMessage = "Некоректний номер телефону.")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }


        [Required(ErrorMessage = "Введи пароль.")]
        [DataType(DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage =
                "Пароль повинен містити щонайменше 6 символів.")]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Підтверди пароль.")]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage =
                "Паролі не збігаються.")]
        [Display(Name = "Підтвердження пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}