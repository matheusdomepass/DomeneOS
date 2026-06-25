using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DomeneOS.Controllers
{
    public class OrdensServicoController : Controller
    {
        private readonly BancoContext _context;

        public OrdensServicoController(BancoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(StatusOrdemServico? status)
        {
            var ordens =  _context.OrdensServico.Include(o => o.Cliente).AsQueryable();

            if (status.HasValue)
            {
                ordens = ordens.Where(o => o.Status == status.Value);
            }

            ViewBag.StatusSelecionado = status;

            return View(await ordens.ToListAsync());
        }

        public IActionResult Criar()
        {
            ViewBag.Clientes = new SelectList(_context.Clientes, "Id", "Nome");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(OrdemServico ordemServico)
        {
            ModelState.Remove("Cliente");

            var valorDigitado = Request.Form["Valor"].ToString();

            if (decimal.TryParse(valorDigitado, new CultureInfo("pt-BR"), out decimal valorConvertido))
            {
                ordemServico.Valor = valorConvertido;
                ModelState.Remove("Valor");
            }
            if (ModelState.IsValid)
            {
                _context.OrdensServico.Add(ordemServico);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Ordem de serviço cadastrada com sucesso";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clientes = new SelectList(_context.Clientes, "Id", "Nome", ordemServico.ClienteId);
            return View(ordemServico);
        }

        public async Task<IActionResult> Detalhes(int id)
        {
            var ordem = await _context.OrdensServico.Include(p => p.Cliente).FirstOrDefaultAsync(p => p.Id == id);

            if (ordem == null)
            {
                return NotFound();
            }
            return View(ordem);
        }

        public async Task<IActionResult> Editar(int id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);

            if (ordem == null)
            {
                return NotFound();
            }

            ViewBag.Clientes = new SelectList(_context.Clientes, "Id", "Nome", ordem.ClienteId);
            return View(ordem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, OrdemServico ordem)
        {
            ModelState.Remove("Cliente");

            var valorDigitado = Request.Form["Valor"].ToString();

            if (decimal.TryParse(valorDigitado, new CultureInfo("pt-BR"), out decimal valorConvertido))
            {
                ordem.Valor = valorConvertido;
                ModelState.Remove("Valor");
            }

            if (id != ordem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var ordemBanco = await _context.OrdensServico.FindAsync(id);

                if (ordemBanco == null)
                {
                    return NotFound();
                }

                ordemBanco.ClienteId = ordem.ClienteId;
                ordemBanco.DescricaoProblema = ordem.DescricaoProblema;
                ordemBanco.Diagnostico = ordem.Diagnostico;
                ordemBanco.Solucao = ordem.Solucao;
                ordemBanco.Valor = ordem.Valor;
                ordemBanco.Status = ordem.Status;

                if (ordem.Status == StatusOrdemServico.Finalizada && ordemBanco.DataFinalizacao == null)
                {
                    ordemBanco.DataFinalizacao = DateTime.Now;
                }

                if (ordem.Status != StatusOrdemServico.Finalizada)
                {
                    ordemBanco.DataFinalizacao = null;
                }

                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Ordem de serviço atualizada com sucesso";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clientes = new SelectList(_context.Clientes, "Id", "Nome", ordem.ClienteId);
            return View(ordem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);

            if (ordem == null)
            {
                return NotFound();
            }

            _context.OrdensServico.Remove(ordem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
