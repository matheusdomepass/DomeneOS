using Microsoft.AspNetCore.Mvc;
using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DomeneOS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly BancoContext _context;

        public HomeController(BancoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalClientes = await _context.Clientes.CountAsync();
            ViewBag.TotalOrdens = await _context.OrdensServico.CountAsync();

            ViewBag.OrdensAbertas = await _context.OrdensServico.CountAsync(o => o.Status == StatusOrdemServico.Aberta);
            ViewBag.OrdensEmAndamento = await _context.OrdensServico.CountAsync(o => o.Status == StatusOrdemServico.EmAndamento);
            ViewBag.OrdensFinalizadas = await _context.OrdensServico.CountAsync(o => o.Status == StatusOrdemServico.Finalizada);
            ViewBag.OrdensCanceladas = await _context.OrdensServico.CountAsync(o => o.Status == StatusOrdemServico.Cancelada);

            var totalServicos = await _context.OrdensServico.Where(o => o.Status == StatusOrdemServico.Finalizada)
            .SumAsync(o => (decimal?)o.Valor) ?? 0;

            var totalProdutos = await _context.OrdemServicoProdutos
                .Where(item =>
                    item.OrdemServico.Status == StatusOrdemServico.Finalizada)
                .SumAsync(item =>
                    (decimal?)(item.Quantidade * item.PrecoUnitario)) ?? 0;

            ViewBag.TotalFaturado = totalServicos + totalProdutos;

            ViewBag.TotalProdutos = await _context.Produtos.CountAsync(p => p.Ativo);

            ViewBag.ProdutosEstoqueBaixo = await _context.Produtos.CountAsync(p => p.Ativo && p.QuantidadeEstoque <= p.EstoqueMinimo);


            return View();
        }


    }
}