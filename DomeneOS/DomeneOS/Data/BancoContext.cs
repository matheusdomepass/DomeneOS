using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DomeneOS.Data
{
    public class BancoContext : IdentityDbContext
    {
        public  BancoContext(DbContextOptions<BancoContext> options) : base(options)
        {
        }
        public DbSet<Cliente> Clientes {  get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
}
}
