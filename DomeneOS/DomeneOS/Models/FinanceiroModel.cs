using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomeneOS.Models
{
    public enum TipoLancamento
    {
        Receita = 1,
        Despesa = 2
    }

    public enum StatusLancamento
    {
        Pendente = 1,
        Pago = 2,
        Cancelado = 3
    }

    public enum FormaPagamento
    {
        NaoInformado = 0,
        Dinheiro = 1,
        Pix = 2,
        CartaoCredito = 3,
        CartaoDebito = 4,
        Boleto = 5,
        Transferencia = 6
    }

    public class LancamentoFinanceiro
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Descricao { get; set; } = string.Empty;

        public TipoLancamento Tipo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        public DateTime DataLancamento { get; set; } = DateTime.Now;

        public DateTime DataVencimento { get; set; }

        public DateTime? DataPagamento { get; set; }

        public StatusLancamento Status { get; set; } = StatusLancamento.Pendente;

        public FormaPagamento FormaPagamento { get; set; }

        // Permite saber se a receita veio de uma OS
        public int? OrdemServicoId { get; set; }

        [ForeignKey(nameof(OrdemServicoId))]
        public OrdemServico? OrdemServico { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }
    }
}