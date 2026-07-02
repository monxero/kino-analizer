using Microsoft.AspNetCore.Mvc.RazorPages;
using KinoAnalyzer.Services;

namespace KinoAnalyzer.Pages.Estadisticas;

public class EstadisticasModel : PageModel
{
    private readonly KinoStatsService _stats;

    public int TotalSorteos { get; set; }
    public List<FrecuenciaNumero> Frecuencias { get; set; } = new();
    public List<NumeroAtrasado> Atrasados { get; set; } = new();

    public EstadisticasModel(KinoStatsService stats)
    {
        _stats = stats;
    }

    public async Task OnGetAsync()
    {
        TotalSorteos = await _stats.ObtenerTotalSorteos();
        Frecuencias = await _stats.ObtenerFrecuencias();
        Atrasados = await _stats.ObtenerAtrasados();
    }
}