using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DomeneOS.Data
{
    public class BancoContext : IdentityDbContext<ApplicationUser>
    {
        public BancoContext(DbContextOptions<BancoContext> options) : base(options)
        {
        }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<LancamentoFinanceiro> LancamentosFinanceiros { get; set;}

        public DbSet<OrdemServicoProduto> OrdemServicoProdutos { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Produto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            builder.Entity<OrdemServicoProduto>()
            .HasIndex(op => new {
            op.OrdemServicoId,
            op.ProdutoId
            }).IsUnique();

            builder.Entity<OrdemServicoProduto>()
                .HasOne(op => op.OrdemServico)
                .WithMany(o => o.ProdutosUtilizados)
                .HasForeignKey(op => op.OrdemServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrdemServicoProduto>()
                .HasOne(op => op.Produto)
                .WithMany(p => p.OrdensServico)
                .HasForeignKey(op => op.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
