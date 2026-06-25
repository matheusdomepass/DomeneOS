using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;

namespace DomeneOS.Data
{
    public class BancoContext : DbContext
    {
        public  BancoContext(DbContextOptions<BancoContext> options) : base(options)
        {
        }
        public DbSet<Cliente> Clientes {  get; set; }
        public DbSet<OrdemServico> OrdensServico { get; set; }
}
}
