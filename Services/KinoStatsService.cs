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


public class DistribucionRango
{
    public string Rango { get; set; } = string.Empty;
    public int Min { get; set; }
    public int Max { get; set; }
    public double Promedio { get; set; }
    public int TotalApariciones { get; set; }
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

    public async Task<List<ParNumeros>> ObtenerPares()
    {
        var pares = await _db.NumerosSorteados
            .Join(_db.NumerosSorteados,
                a => a.SorteoId,
                b => b.SorteoId,
                (a, b) => new { a, b })
            .Where(x => x.a.Numero < x.b.Numero)
            .GroupBy(x => new { Num1 = x.a.Numero, Num2 = x.b.Numero })
            .Select(g => new ParNumeros
            {
                Num1 = g.Key.Num1,
                Num2 = g.Key.Num2,
                Veces = g.Count()
            })
            .OrderByDescending(p => p.Veces)
            .Take(20)
            .ToListAsync();

        return pares;
    }

    public async Task<List<DistribucionRango>> ObtenerDistribucionRangos()
    {
        var total = await ObtenerTotalSorteos();
        var numeros = await _db.NumerosSorteados.ToListAsync();

        var rangos = new List<DistribucionRango>
        {
            new() {
                Rango = "Bajos (1-8)", Min = 1, Max = 8,
                TotalApariciones = numeros.Count(n => n.Numero >= 1 && n.Numero <= 8),
                Promedio = Math.Round((double)numeros.Count(n => n.Numero >= 1 && n.Numero <= 8) / total, 1)
            },
            new() {
                Rango = "Medios (9-17)", Min = 9, Max = 17,
                TotalApariciones = numeros.Count(n => n.Numero >= 9 && n.Numero <= 17),
                Promedio = Math.Round((double)numeros.Count(n => n.Numero >= 9 && n.Numero <= 17) / total, 1)
            },
            new() {
                Rango = "Altos (18-25)", Min = 18, Max = 25,
                TotalApariciones = numeros.Count(n => n.Numero >= 18 && n.Numero <= 25),
                Promedio = Math.Round((double)numeros.Count(n => n.Numero >= 18 && n.Numero <= 25) / total, 1)
            }
        };

        return rangos;
    }

    public async Task<object> AnalizarCombinacion(List<int> numeros)
    {
        var sorteos = await _db.Sorteos
            .Include(s => s.Numeros)
            .ToListAsync();

        var resultados = sorteos.Select(s => {
            var coincidencias = s.Numeros.Count(n => numeros.Contains(n.Numero));
            return new {
                NumeroSorteo = s.NumeroSorteo,
                Fecha = s.FechaSorteo.ToString("dd/MM/yyyy"),
                Coincidencias = coincidencias
            };
        })
        .OrderByDescending(r => r.Coincidencias)
        .Take(10)
        .ToList();

        var promedio = sorteos.Average(s =>
            s.Numeros.Count(n => numeros.Contains(n.Numero)));

        return new {
            PromedioCoincidencias = Math.Round(promedio, 1),
            MejoresSorteos = resultados
        };
    }
}