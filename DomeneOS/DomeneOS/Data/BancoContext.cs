using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DomeneOS.Data
{
    public class BancoContext : IdentityDbContext<ApplicationUser>
    {
        public  BancoContext(DbContextOptions<BancoContext> options) : base(options)
        {
        }
        public DbSet<Cliente> Clientes {  get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Produto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();
        }
    }
}
