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
        private readonly IWebHostEnvironment _environment;

        public OrdensServicoController(BancoContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(StatusOrdemServico? status, string? pesquisa)
        {
            var ordens = _context.OrdensServico
                .Include(o => o.Cliente).Include(o => o.ProdutosUtilizados)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                pesquisa = pesquisa.Trim();

                var pesquisaEhNumero = int.TryParse(pesquisa, out var numeroOrdem);

                ordens = ordens.Where(o =>
                    o.Cliente.Nome.Contains(pesquisa) ||
                    o.DescricaoProblema.Contains(pesquisa) ||
                    (pesquisaEhNumero && o.Id == numeroOrdem));
            }

            if (status.HasValue)
            {
                ordens = ordens.Where(o => o.Status == status.Value);
            }

            ViewBag.Pesquisa = pesquisa;
            ViewBag.StatusSelecionado = status?.ToString();

            return View(await ordens
                .OrderByDescending(o => o.DataAbertura)
                .ToListAsync());
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
            var ordem = await _context.OrdensServico
                .Include(o => o.Cliente)
                .Include(o => o.ProdutosUtilizados)
                    .ThenInclude(op => op.Produto)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null)
            {
                return NotFound();
            }

            ViewBag.ProdutosDisponiveis = await _context.Produtos
                .Where(p => p.Ativo && p.QuantidadeEstoque > 0)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return View(ordem);
        }
        public async Task<IActionResult> Editar(int id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);

            if (ordem == null)
            {
                return NotFound();
            }

            if (ordem.Status == StatusOrdemServico.Finalizada ||
                ordem.Status == StatusOrdemServico.Cancelada)
            {
                TempData["Erro"] =
                    "Ordens finalizadas ou canceladas não podem ser editadas.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = ordem.Id });
            }

            ViewBag.Clientes = new SelectList(
                _context.Clientes,
                "Id",
                "Nome",
                ordem.ClienteId);

            return View(ordem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, OrdemServico ordem)
        {
            ModelState.Remove("Cliente");

            var valorDigitado = Request.Form["Valor"].ToString();

            if (decimal.TryParse(
                valorDigitado,
                new CultureInfo("pt-BR"),
                out decimal valorConvertido))
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
                var ordemBanco = await _context.OrdensServico
                    .Include(o => o.ProdutosUtilizados)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (ordemBanco == null)
                {
                    return NotFound();
                }
                if (ordemBanco.Status == StatusOrdemServico.Finalizada ||
                    ordemBanco.Status == StatusOrdemServico.Cancelada)
                {
                    TempData["Erro"] =
                        "Ordens finalizadas ou canceladas não podem ser editadas.";

                    return RedirectToAction(
                        nameof(Detalhes),
                        new { id = ordemBanco.Id });
                }
                var statusAnterior = ordemBanco.Status;

                ordemBanco.ClienteId = ordem.ClienteId;
                ordemBanco.DescricaoProblema = ordem.DescricaoProblema;
                ordemBanco.Diagnostico = ordem.Diagnostico;
                ordemBanco.Solucao = ordem.Solucao;
                ordemBanco.Valor = ordem.Valor;
                ordemBanco.Status = ordem.Status;

                if (ordem.Status == StatusOrdemServico.Finalizada &&
                    statusAnterior != StatusOrdemServico.Finalizada)
                {
                    ordemBanco.DataFinalizacao = DateTime.Now;

                    var totalProdutos = ordemBanco.ProdutosUtilizados
                        .Sum(p => p.Quantidade * p.PrecoUnitario);

                    var valorTotal = ordemBanco.Valor + totalProdutos;

                    var lancamentoExistente =
                        await _context.LancamentosFinanceiros
                            .AnyAsync(l => l.OrdemServicoId == ordemBanco.Id);

                    if (!lancamentoExistente)
                    {
                        var lancamento = new LancamentoFinanceiro
                        {
                            Descricao = $"Ordem de Serviço #{ordemBanco.Id}",
                            Tipo = TipoLancamento.Receita,
                            Valor = valorTotal,
                            DataLancamento = DateTime.Now,
                            DataVencimento = DateTime.Today,
                            Status = StatusLancamento.Pendente,
                            FormaPagamento = FormaPagamento.NaoInformado,
                            OrdemServicoId = ordemBanco.Id
                        };

                        _context.LancamentosFinanceiros.Add(lancamento);
                    }
                }
                else
                {
                    ordemBanco.DataFinalizacao = null;
                }

                await _context.SaveChangesAsync();

                TempData["Sucesso"] =
                    "Ordem de serviço atualizada com sucesso";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clientes = new SelectList(
                _context.Clientes,
                "Id",
                "Nome",
                ordem.ClienteId);

            return View(ordem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var ordem = await _context.OrdensServico.Include(o => o.ProdutosUtilizados).ThenInclude(op => op.Produto).FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null)
            {
                return NotFound();
            }

            foreach (var item in ordem.ProdutosUtilizados)
            {
                item.Produto.QuantidadeEstoque += item.Quantidade;
            }

            _context.OrdensServico.Remove(ordem);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Ordem de serviço excluída e produtos devolvidos ao estoque.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GerarPDF(int id)
        {
            var ordem = await _context.OrdensServico.Include(o => o.Cliente).Include(o => o.ProdutosUtilizados)
                    .ThenInclude(item => item.Produto).FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null)
            {
                return NotFound();
            }

            var totalProdutos = ordem.ProdutosUtilizados
                .Sum(item => item.Quantidade * item.PrecoUnitario);

            var totalOrdem = ordem.Valor + totalProdutos;

            var caminhoLogo = Path.Combine(_environment.WebRootPath,"images","logo Domenetech.png");

            byte[]? logoBytes = null;

            if (System.IO.File.Exists(caminhoLogo))
            {
                logoBytes = System.IO.File.ReadAllBytes(caminhoLogo);
            }

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header().PaddingBottom(15).Row(row => {
                        if (logoBytes != null)
                    {
                        row.ConstantItem(70)
                            .Height(70)
                            .Image(logoBytes)
                            .FitArea();
                    }

                    row.RelativeItem()
                        .PaddingLeft(15)
                        .AlignMiddle()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("DomeneOS")
                                .FontSize(20)
                                .Bold();

                            column.Item()
                                .Text("Sistema de Gestão de Ordens de Serviço")
                                .FontSize(10);

                            column.Item()
                                .Text("DomeneTech")
                                .FontSize(9);
                        });
                }); ;

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item()
                            .Text($"OS N°: {ordem.Id}")
                            .Bold();

                        col.Item().Text($"Cliente: {ordem.Cliente.Nome}");
                        col.Item().Text($"Telefone: {ordem.Cliente.Telefone}");
                        col.Item().Text($"E-mail: {ordem.Cliente.Email}");
                        col.Item().Text($"CPF/CNPJ: {ordem.Cliente.CpfCnpj}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text(
                            $"Descrição do problema: {ordem.DescricaoProblema}");

                        col.Item().Text(
                            $"Diagnóstico: {ordem.Diagnostico ?? "Não informado"}");

                        col.Item().Text(
                            $"Solução: {ordem.Solucao ?? "Não informada"}");

                        col.Item().LineHorizontal(1);

                        col.Item()
                            .Text("Produtos utilizados")
                            .FontSize(13)
                            .Bold();

                        if (ordem.ProdutosUtilizados.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .PaddingBottom(5)
                                        .Text("Produto")
                                        .Bold();

                                    header.Cell()
                                        .AlignCenter()
                                        .PaddingBottom(5)
                                        .Text("Qtd.")
                                        .Bold();

                                    header.Cell()
                                        .AlignRight()
                                        .PaddingBottom(5)
                                        .Text("Valor unitário")
                                        .Bold();

                                    header.Cell()
                                        .AlignRight()
                                        .PaddingBottom(5)
                                        .Text("Subtotal")
                                        .Bold();
                                });

                                foreach (var item in ordem.ProdutosUtilizados)
                                {
                                    var subtotal =
                                        item.Quantidade * item.PrecoUnitario;

                                    table.Cell()
                                        .PaddingVertical(4)
                                        .Text(item.Produto.Nome);

                                    table.Cell()
                                        .AlignCenter()
                                        .PaddingVertical(4)
                                        .Text(item.Quantidade.ToString());

                                    table.Cell()
                                        .AlignRight()
                                        .PaddingVertical(4)
                                        .Text(item.PrecoUnitario.ToString("C"));

                                    table.Cell()
                                        .AlignRight()
                                        .PaddingVertical(4)
                                        .Text(subtotal.ToString("C"));
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Nenhum produto utilizado.");
                        }

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Status: {ordem.Status}");

                        col.Item().Text(
                            $"Valor da mão de obra: {ordem.Valor:C}");

                        col.Item().Text(
                            $"Total dos produtos: {totalProdutos:C}");

                        col.Item()
                            .Text($"Total da ordem: {totalOrdem:C}")
                            .FontSize(14)
                            .Bold();

                        col.Item().Text(
                            $"Data de abertura: {ordem.DataAbertura:dd/MM/yyyy HH:mm}");

                        col.Item().Text(
                            $"Data de finalização: " +
                            $"{(ordem.DataFinalizacao.HasValue
                                ? ordem.DataFinalizacao.Value.ToString("dd/MM/yyyy HH:mm")
                                : "Não finalizada")}");
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("Documento gerado pelo DomeneOS");
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"OS-{ordem.Id}.pdf");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarProduto(int ordemServicoId, int produtoId, int quantidade)
        {
            if (quantidade <= 0)
            {
                TempData["Erro"] = "Informe uma quantidade maior que zero.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = ordemServicoId });
            }

            var ordem = await _context.OrdensServico
                .FirstOrDefaultAsync(o => o.Id == ordemServicoId);

            if (ordem == null)
            {
                return NotFound();
            }

            if (ordem.Status == StatusOrdemServico.Finalizada ||
                ordem.Status == StatusOrdemServico.Cancelada)
            {
                TempData["Erro"] =
                    "Não é possível adicionar produtos a uma ordem finalizada ou cancelada.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = ordemServicoId });
            }

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.Id == produtoId && p.Ativo);

            if (produto == null)
            {
                TempData["Erro"] = "Produto não encontrado ou inativo.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = ordemServicoId });
            }

            if (produto.QuantidadeEstoque < quantidade)
            {
                TempData["Erro"] =
                    $"Estoque insuficiente. Quantidade disponível: {produto.QuantidadeEstoque}.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = ordemServicoId });
            }

            var itemExistente = await _context.OrdemServicoProdutos
                .FirstOrDefaultAsync(op =>
                    op.OrdemServicoId == ordemServicoId &&
                    op.ProdutoId == produtoId);

            if (itemExistente == null)
            {
                var novoItem = new OrdemServicoProduto
                {
                    OrdemServicoId = ordemServicoId,
                    ProdutoId = produtoId,
                    Quantidade = quantidade,
                    PrecoUnitario = produto.PrecoVenda
                };

                _context.OrdemServicoProdutos.Add(novoItem);
            }
            else
            {
                itemExistente.Quantidade += quantidade;
            }

            produto.QuantidadeEstoque -= quantidade;

            if (ordem.Status == StatusOrdemServico.Aberta)
            {
                ordem.Status = StatusOrdemServico.EmAndamento;
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Produto adicionado à ordem de serviço.";

            return RedirectToAction(nameof(Detalhes), new { id = ordemServicoId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverProduto(int id)
        {
            var item = await _context.OrdemServicoProdutos
                .Include(op => op.Produto)
                .Include(op => op.OrdemServico)
                .FirstOrDefaultAsync(op => op.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            if (item.OrdemServico.Status == StatusOrdemServico.Finalizada ||
                item.OrdemServico.Status == StatusOrdemServico.Cancelada)
            {
                TempData["Erro"] =
                    "Não é possível remover produtos de uma ordem finalizada ou cancelada.";

                return RedirectToAction(
                    nameof(Detalhes),
                    new { id = item.OrdemServicoId });
            }

            var ordemServicoId = item.OrdemServicoId;

            item.Produto.QuantidadeEstoque += item.Quantidade;

            _context.OrdemServicoProdutos.Remove(item);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] =
                "Produto removido e quantidade devolvida ao estoque.";

            return RedirectToAction(
                nameof(Detalhes),
                new { id = ordemServicoId });
        }
    }
}
