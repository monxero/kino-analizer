using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KinoAnalyzer.Data;
using KinoAnalyzer.Models;

namespace KinoAnalyzer.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public int TotalSorteos { get; set; }
    public int UltimoSorteo { get; set; }
    public int PrimerSorteo { get; set; }
    public List<Sorteo> UltimosSorteos { get; set; } = new();

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        TotalSorteos = await _db.Sorteos.CountAsync();
        UltimoSorteo = await _db.Sorteos.MaxAsync(s => s.NumeroSorteo);
        PrimerSorteo = await _db.Sorteos.MinAsync(s => s.NumeroSorteo);

        UltimosSorteos = await _db.Sorteos
            .Include(s => s.Numeros)
            .OrderByDescending(s => s.NumeroSorteo)
            .Take(10)
            .ToListAsync();
    }
}