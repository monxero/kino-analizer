using Microsoft.EntityFrameworkCore;
using KinoAnalyzer.Data;

namespace KinoAnalyzer.Services;

public class FrecuenciaNumero
{
    public int Numero { get; set; }
    public int Veces { get; set; }
    public double Frecuencia { get; set; }
    public double Diferencia { get; set; }
}

public class NumeroAtrasado
{
    public int Numero { get; set; }
    public int UltimoSorteo { get; set; }
    public int SorteosSinSalir { get; set; }
}

public class ParNumeros
{
    public int Num1 { get; set; }
    public int Num2 { get; set; }
    public int Veces { get; set; }
}

public class KinoStatsService
{
    private readonly AppDbContext _db;

    public KinoStatsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> ObtenerTotalSorteos()
    {
        return await _db.Sorteos.CountAsync();
    }

    public async Task<List<FrecuenciaNumero>> ObtenerFrecuencias()
    {
        var total = await ObtenerTotalSorteos();
        var probabilidadEsperada = 14.0 / 25.0;

        var frecuencias = await _db.NumerosSorteados
            .GroupBy(n => n.Numero)
            .Select(g => new FrecuenciaNumero
            {
                Numero = g.Key,
                Veces = g.Count(),
                Frecuencia = Math.Round((double)g.Count() / total * 100, 1),
                Diferencia = Math.Round((double)g.Count() / total * 100 - probabilidadEsperada * 100, 1)
            })
            .OrderByDescending(f => f.Veces)
            .ToListAsync();

        return frecuencias;
    }

    public async Task<List<NumeroAtrasado>> ObtenerAtrasados()
    {
        var ultimoSorteo = await _db.Sorteos.MaxAsync(s => s.NumeroSorteo);

        var atrasados = await _db.NumerosSorteados
            .GroupBy(n => n.Numero)
            .Select(g => new NumeroAtrasado
            {
                Numero = g.Key,
                UltimoSorteo = g.Max(n => n.Sorteo.NumeroSorteo),
                SorteosSinSalir = ultimoSorteo - g.Max(n => n.Sorteo.NumeroSorteo)
            })
            .OrderByDescending(a => a.SorteosSinSalir)
            .ToListAsync();

        return atrasados;
    }
}