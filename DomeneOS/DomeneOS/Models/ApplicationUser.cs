using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DomeneOS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
    }
}
