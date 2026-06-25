using System.ComponentModel.DataAnnotations;

namespace DomeneOS.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Telefone inválido")]
        public string Telefone { get; set; } = string.Empty;
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "O CPF ou CNPJ é obrigatório.")]
        [StringLength(18, MinimumLength = 14, ErrorMessage = "CPF/CNPJ inválido")]
        public string CpfCnpj { get; set; } = string.Empty;
        public string? Endereco { get; set; }
        public ICollection<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
    }
}
