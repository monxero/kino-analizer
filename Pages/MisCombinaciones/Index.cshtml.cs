using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KinoAnalyzer.Data;
using KinoAnalyzer.Models;
using System.Security.Claims;

namespace KinoAnalyzer.Pages.MisCombinaciones;

[Authorize]
public class MisCombinacionesModel : PageModel
{
    private readonly AppDbContext _db;

    public List<CombinacionUsuario> Combinaciones { get; set; } = new();

    public MisCombinacionesModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        Combinaciones = await _db.CombinacionesUsuario
            .Include(c => c.Numeros)
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.EsFavorita)
            .ThenByDescending(c => c.CreadoEn)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(string Nombre, string Numeros, string? Notas)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var numerosLista = Numeros.Split(',')
            .Select(n => n.Trim())
            .Where(n => int.TryParse(n, out _))
            .Select(int.Parse)
            .Distinct()
            .Where(n => n >= 1 && n <= 25)
            .Take(14)
            .ToList();

        if (numerosLista.Count != 14)
        {
            ModelState.AddModelError("", "Debés ingresar exactamente 14 números del 1 al 25.");
            await OnGetAsync();
            return Page();
        }

        var combinacion = new CombinacionUsuario
        {
            UsuarioId = usuarioId!,
            Nombre = Nombre,
            Notas = Notas,
            EsFavorita = false,
            CreadoEn = DateTime.Now,
            Numeros = numerosLista.Select(n => new NumeroCombinacion { Numero = n }).ToList()
        };

        _db.CombinacionesUsuario.Add(combinacion);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var combinacion = await _db.CombinacionesUsuario
            .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);

        if (combinacion != null)
        {
            _db.CombinacionesUsuario.Remove(combinacion);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}