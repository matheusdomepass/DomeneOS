using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomeneOS.Models
{
    public class OrdemServicoProduto
    {
        public int Id { get; set; }

        public int OrdemServicoId { get; set; }

        public OrdemServico OrdemServico { get; set; } = null!;

        public int ProdutoId { get; set; }

        public Produto Produto { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecoUnitario { get; set; }

        [NotMapped]
        public decimal Subtotal => Quantidade * PrecoUnitario;
    }
}