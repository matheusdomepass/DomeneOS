using DomeneOS.Data;
using DomeneOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DomeneOS.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly BancoContext _context;

        public ClientesController(BancoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string pesquisa)
        {
            var clientes = _context.Clientes.AsQueryable();

            if (!string.IsNullOrEmpty(pesquisa))
            {
                clientes = clientes.Where(c => c.Nome.Contains(pesquisa) || c.CpfCnpj.Contains(pesquisa));
            }

            ViewBag.Pesquisa = pesquisa;
            return View(await clientes.ToListAsync());
        }

        public IActionResult Criar()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                bool cpfCnpjExistente = await _context.Clientes.AnyAsync(c => c.CpfCnpj == cliente.CpfCnpj);

                if (cpfCnpjExistente)
                {
                    ModelState.AddModelError("CpfCnpj", "Já existe um cliente cadastrado com esse CPF/CNPJ");

                    return View(cliente);
                }

                bool emailExiste = await _context.Clientes.AnyAsync(c => c.Email == cliente.Email);

                if (emailExiste)
                {
                    ModelState.AddModelError("Email", "Já existe um cliente cadastrado com este e-mail.");

                    return View(cliente);
                }

                _context.Add(cliente);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Cliente cadastrado com sucesso";

                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }
        public async Task<IActionResult> Detalhes(int id)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(x => x.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }
        public async Task<IActionResult> Editar(int id)
        {
            var cliente = await (_context.Clientes.FindAsync(id));

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Cliente cliente)
        {

            if (id != cliente.Id)
            {
                return NotFound();
            }

            bool cpfCnpjExiste = await _context.Clientes.AnyAsync(c => c.CpfCnpj == cliente.CpfCnpj
                && c.Id != cliente.Id);

            if (cpfCnpjExiste)
            {
                ModelState.AddModelError("CpfCnpj", "Já existe um cliente cadastrado com este CPF/CNPJ.");

                return View(cliente);
            }

            bool emailExiste = await _context.Clientes.AnyAsync(c => c.Email == cliente.Email
                && c.Id != cliente.Id);

            if (emailExiste)
            {
                ModelState.AddModelError("Email",
                    "Já existe um cliente cadastrado com este e-mail.");

                return View(cliente);
            }

            if (ModelState.IsValid)
            {
                _context.Update(cliente);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Cliente atualizado com sucesso";

                return RedirectToAction(nameof(Index));
            }

            return View(cliente);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            bool possuiOrdens = await _context.OrdensServico
                .AnyAsync(o => o.ClienteId == id);

            if (possuiOrdens)
            {
                TempData["Erro"] = "Não é possível excluir este cliente, pois ele possui ordens de serviço vinculadas.";
                return RedirectToAction(nameof(Index));
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Cliente excluído com sucesso.";
            return RedirectToAction(nameof(Index));
        }
    }
}