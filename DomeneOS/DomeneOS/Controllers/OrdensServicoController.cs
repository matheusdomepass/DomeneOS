using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace DomeneOS.Controllers
{
    [Authorize]
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

        public async Task<IActionResult> GerarPDF(int id)
        {
            var ordem = await _context.OrdensServico.Include(o => o.Cliente).FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null)
            {
                return NotFound();
            }

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header().Text("DomeneOS - Ordem de Serviço").FontSize(20).Bold();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"OS N°: {ordem.Id}").Bold();
                        col.Item().Text($"Cliente: {ordem.Cliente.Nome}");
                        col.Item().Text($"Telefone: {ordem.Cliente.Telefone}");
                        col.Item().Text($"Email: {ordem.Cliente.Email}");
                        col.Item().Text($"CPF/CNPJ: {ordem.Cliente.CpfCnpj}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Descrição do Problema: {ordem.DescricaoProblema}");
                        col.Item().Text($"Diagnóstico: {ordem.Diagnostico ?? "Não informado"}");
                        col.Item().Text($"Solução: {ordem.Solucao ?? "Não informado"}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Status: {ordem.Status}");
                        col.Item().Text($"Valor: {ordem.Valor.ToString("C")}");
                        col.Item().Text($"Data de Abertura: {ordem.DataAbertura:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"Data de Finalização: {(ordem.DataFinalizacao.HasValue ? ordem.DataFinalizacao.Value.ToString("dd/MM/yyyy") : "Não finalizada")}");

                    });

                    page.Footer().AlignCenter().Text("Documento gerado com sucesso");
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"OS-{ordem.Id}.pdf");
        }
    }
}
