using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomeneOS.Controllers
{
    [Authorize]
    public class FinanceiroController : Controller
    {
        private readonly BancoContext _context;

        public FinanceiroController(BancoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
    string? pesquisa,
    TipoLancamento? tipo,
    StatusLancamento? status)
        {
            // Consulta usada para calcular os cards.
            // Os cards continuam mostrando o resumo financeiro geral,
            // independentemente dos filtros da tabela.
            var todosLancamentos = await _context.LancamentosFinanceiros
                .AsNoTracking()
                .ToListAsync();

            var inicioMes = new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                1);

            var fimMes = inicioMes.AddMonths(1);

            ViewBag.RecebidoMes = todosLancamentos
                .Where(l =>
                    l.Tipo == TipoLancamento.Receita &&
                    l.Status == StatusLancamento.Pago &&
                    l.DataPagamento >= inicioMes &&
                    l.DataPagamento < fimMes)
                .Sum(l => l.Valor);

            ViewBag.AReceber = todosLancamentos
                .Where(l =>
                    l.Tipo == TipoLancamento.Receita &&
                    l.Status == StatusLancamento.Pendente)
                .Sum(l => l.Valor);

            ViewBag.APagar = todosLancamentos
                .Where(l =>
                    l.Tipo == TipoLancamento.Despesa &&
                    l.Status == StatusLancamento.Pendente)
                .Sum(l => l.Valor);

            var despesasPagasMes = todosLancamentos
                .Where(l =>
                    l.Tipo == TipoLancamento.Despesa &&
                    l.Status == StatusLancamento.Pago &&
                    l.DataPagamento >= inicioMes &&
                    l.DataPagamento < fimMes)
                .Sum(l => l.Valor);

            ViewBag.SaldoMes =
                ViewBag.RecebidoMes - despesasPagasMes;

            // Consulta da tabela
            var lancamentos = _context.LancamentosFinanceiros
                .Include(l => l.OrdemServico)
                .AsQueryable();

            // PESQUISA
            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                pesquisa = pesquisa.Trim();

                var pesquisaEhNumero =
                    int.TryParse(pesquisa, out var numero);

                lancamentos = lancamentos.Where(l =>
                    l.Descricao.Contains(pesquisa) ||
                    (pesquisaEhNumero &&
                     l.OrdemServicoId == numero));
            }

            // TIPO
            if (tipo.HasValue)
            {
                lancamentos = lancamentos
                    .Where(l => l.Tipo == tipo.Value);
            }

            // STATUS
            if (status.HasValue)
            {
                lancamentos = lancamentos
                    .Where(l => l.Status == status.Value);
            }

            ViewBag.Pesquisa = pesquisa;
            ViewBag.TipoSelecionado = tipo?.ToString();
            ViewBag.StatusSelecionado = status?.ToString();

            return View(
                await lancamentos
                    .OrderByDescending(l => l.DataLancamento)
                    .ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(LancamentoFinanceiro lancamento)
        {
            if (!ModelState.IsValid)
                return View(lancamento);

            lancamento.DataLancamento = DateTime.Now;
            lancamento.Status = StatusLancamento.Pendente;
            lancamento.DataPagamento = null;

            _context.LancamentosFinanceiros.Add(lancamento);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Lançamento cadastrado com sucesso!";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> RegistrarPagamento(int id)
        {
            var lancamento = await _context.LancamentosFinanceiros
                .Include(l => l.OrdemServico)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pendente)
            {
                TempData["Erro"] = "Este lançamento não está pendente.";
                return RedirectToAction(nameof(Index));
            }

            lancamento.DataPagamento = DateTime.Now;

            return View(lancamento);
        }
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var lancamento = await _context.LancamentosFinanceiros
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pendente)
            {
                TempData["Erro"] = "Somente lançamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Index));
            }

            // Lançamentos gerados por OS não podem ser editados pelo Financeiro
            if (lancamento.OrdemServicoId.HasValue)
            {
                TempData["Erro"] =
                    "Lançamentos gerados por uma ordem de serviço devem ser alterados pela própria OS.";

                return RedirectToAction(nameof(Index));
            }

            return View(lancamento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var lancamento = await _context.LancamentosFinanceiros
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pendente)
            {
                TempData["Erro"] =
                    "Somente lançamentos pendentes podem ser cancelados.";

                return RedirectToAction(nameof(Index));
            }

            lancamento.Status = StatusLancamento.Cancelado;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Lançamento cancelado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EstornarPagamento(int id)
        {
            var lancamento = await _context.LancamentosFinanceiros
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pago)
            {
                TempData["Erro"] =
                    "Somente lançamentos pagos podem ser estornados.";

                return RedirectToAction(nameof(Index));
            }

            lancamento.Status = StatusLancamento.Pendente;
            lancamento.DataPagamento = null;
            lancamento.FormaPagamento = FormaPagamento.NaoInformado;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Pagamento estornado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, LancamentoFinanceiro model)
        {
            if (id != model.Id)
                return NotFound();

            var lancamento = await _context.LancamentosFinanceiros
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pendente)
            {
                TempData["Erro"] = "Somente lançamentos pendentes podem ser editados.";
                return RedirectToAction(nameof(Index));
            }

            if (lancamento.OrdemServicoId.HasValue)
            {
                TempData["Erro"] =
                    "Lançamentos gerados por uma ordem de serviço não podem ser editados pelo Financeiro.";

                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("OrdemServico");

            if (!ModelState.IsValid)
                return View(model);

            lancamento.Descricao = model.Descricao;
            lancamento.Tipo = model.Tipo;
            lancamento.Valor = model.Valor;
            lancamento.DataVencimento = model.DataVencimento;
            lancamento.FormaPagamento = model.FormaPagamento;
            lancamento.Observacao = model.Observacao;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Lançamento atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPagamento(int id, FormaPagamento formaPagamento, DateTime dataPagamento)
        {
            var lancamento = await _context.LancamentosFinanceiros
                .Include(l => l.OrdemServico)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lancamento == null)
                return NotFound();

            if (lancamento.Status != StatusLancamento.Pendente)
            {
                TempData["Erro"] = "Este lançamento não está pendente.";
                return RedirectToAction(nameof(Index));
            }

            // Forma de pagamento é obrigatória
            if (formaPagamento == FormaPagamento.NaoInformado)
            {
                ModelState.AddModelError(
                    "FormaPagamento",
                    "Selecione uma forma de pagamento.");

                lancamento.DataPagamento = dataPagamento;

                return View(lancamento);
            }

            lancamento.Status = StatusLancamento.Pago;
            lancamento.FormaPagamento = formaPagamento;
            lancamento.DataPagamento = dataPagamento;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Pagamento registrado com sucesso!";

            return RedirectToAction(nameof(Index));
        }
    }
}