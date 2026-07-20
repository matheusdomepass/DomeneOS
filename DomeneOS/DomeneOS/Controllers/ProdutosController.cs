using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomeneOS.Controllers
{
    [Authorize]
    public class ProdutosController : Controller
    {
        private readonly BancoContext _context;

        public ProdutosController(BancoContext context)
        {
            _context = context;
        }

        // GET: Produtos
        public async Task<IActionResult> Index(string? busca, bool mostrarInativos = false)
        {
            var produtos = _context.Produtos.AsQueryable();

            if (!mostrarInativos)
            {
                produtos = produtos.Where(p => p.Ativo);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();

                produtos = produtos.Where(p =>
                    p.Nome.Contains(busca) ||
                    p.Codigo.Contains(busca) ||
                    (p.Fornecedor != null && p.Fornecedor.Contains(busca)));
            }

            ViewBag.Busca = busca;
            ViewBag.MostrarInativos = mostrarInativos;

            return View(await produtos
                .OrderBy(p => p.Nome)
                .ToListAsync());
        }

        // GET: Produtos/Details/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // GET: Produtos/Create
        public IActionResult Criar()
        {
            var ultimoId = _context.Produtos.Any()? _context.Produtos.Max(p => p.Id) + 1 : 1;

            ViewBag.ProximoCodigo = $"PROD-{ultimoId:0000}";
            return View();
        }

        // POST: Produtos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Produto produto)
        {
            produto.Codigo = produto.Codigo.Trim();
            produto.Nome = produto.Nome.Trim();

            var codigoExiste = await _context.Produtos
                .AnyAsync(p => p.Codigo == produto.Codigo);

            if (codigoExiste)
            {
                ModelState.AddModelError(
                    nameof(produto.Codigo),
                    "Já existe um produto cadastrado com este código.");
            }

            if (!ModelState.IsValid)
            {
                return View(produto);
            }

            produto.Ativo = true;

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Produto cadastrado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Produtos/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos.FindAsync(id);

            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // POST: Produtos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Produto produto)
        {
            if (id != produto.Id)
            {
                return NotFound();
            }

            produto.Codigo = produto.Codigo.Trim();
            produto.Nome = produto.Nome.Trim();

            var codigoExiste = await _context.Produtos
                .AnyAsync(p =>
                    p.Codigo == produto.Codigo &&
                    p.Id != produto.Id);

            if (codigoExiste)
            {
                ModelState.AddModelError(
                    nameof(produto.Codigo),
                    "Já existe outro produto cadastrado com este código.");
            }

            if (!ModelState.IsValid)
            {
                return View(produto);
            }

            try
            {
                _context.Produtos.Update(produto);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Produto atualizado com sucesso.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProdutoExiste(produto.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Produtos/AlterarStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);

            if (produto == null)
            {
                return NotFound();
            }

            produto.Ativo = !produto.Ativo;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = produto.Ativo
                ? "Produto ativado com sucesso."
                : "Produto desativado com sucesso.";

            return RedirectToAction(nameof(Index),
                new { mostrarInativos = true });
        }

        private bool ProdutoExiste(int id)
        {
            return _context.Produtos.Any(p => p.Id == id);
        }
    }
}