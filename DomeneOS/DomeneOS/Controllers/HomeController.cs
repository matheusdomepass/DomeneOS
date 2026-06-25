using Microsoft.AspNetCore.Mvc;
using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.EntityFrameworkCore;

namespace DomeneOS.Controllers
{
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

            ViewBag.TotalFaturado = await _context.OrdensServico.Where(o => o.Status == StatusOrdemServico.Finalizada).SumAsync(o => o.Valor);

            return View();
        }

    }
}