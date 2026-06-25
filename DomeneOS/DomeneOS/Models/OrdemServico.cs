using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomeneOS.Models
{
    public class OrdemServico
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "A descrição do produto é obrigatória")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "A descrição do problema deve ter entre 5 e 500 caracteres")]
        public string DescricaoProblema { get; set; } = string.Empty;
        [StringLength(500)]
        public string? Diagnostico { get; set; }
        [StringLength(500)]
        public string? Solucao { get; set; }
        [Range(0.01, 99999999.99, ErrorMessage = "O valor deve ser maior que zero")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }
        public DateTime DataAbertura { get; set; } = DateTime.Now;
        public DateTime? DataFinalizacao { get; set; }
        public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Aberta;
        [Required(ErrorMessage = "Selecione um cliente")]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }

    public enum StatusOrdemServico
    {
        Aberta = 1,
        EmAndamento = 2,
        Finalizada = 3,
        Cancelada = 4

    }
}
