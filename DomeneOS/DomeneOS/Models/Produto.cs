using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomeneOS.Models
{
    public class Produto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O código é obrigatório.")]
        [StringLength(30, ErrorMessage = "O código deve ter no máximo 30 caracteres.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A quantidade não pode ser negativa.")]
        [Display(Name = "Quantidade em estoque")]
        public int QuantidadeEstoque { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "O estoque mínimo não pode ser negativo.")]
        [Display(Name = "Estoque mínimo")]
        public int EstoqueMinimo { get; set; } = 0;

        [Range(0, 9999999.99, ErrorMessage = "Informe um preço de compra válido.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Preço de compra")]
        public decimal PrecoCompra { get; set; } = 0;

        [Range(0, 9999999.99, ErrorMessage = "Informe um preço de venda válido.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Preço de venda")]
        public decimal PrecoVenda { get; set; } = 0;

        [StringLength(100)]
        public string? Fornecedor { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<OrdemServicoProduto> OrdensServico { get; set; } = new List<OrdemServicoProduto>();

        [NotMapped]
        public bool EstoqueBaixo =>
            QuantidadeEstoque <= EstoqueMinimo;
    }
}