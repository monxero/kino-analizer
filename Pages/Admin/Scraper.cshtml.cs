using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KinoAnalyzer.Services;
using KinoAnalyzer.Models;

namespace KinoAnalyzer.Pages.Admin;

public class ResultadoScraper
{
    public int Guardados { get; set; }
    public int Saltados { get; set; }
}

public class ScraperModel : PageModel
{
    private readonly ScraperService _scraper;

    public ResultadoScraper? Resultado { get; set; }
    public string? Error { get; set; }

    public ScraperModel(ScraperService scraper)
    {
        _scraper = scraper;
    }

    public void OnGet()
    {
        // Solo muestra la página, no hace nada
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var (guardados, saltados) = await _scraper.ScraperCompleto();
            Resultado = new ResultadoScraper
            {
                Guardados = guardados,
                Saltados = saltados
            };
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }

        return Page();
    }
}