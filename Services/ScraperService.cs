using HtmlAgilityPack;
using KinoAnalyzer.Data;
using KinoAnalyzer.Models;

namespace KinoAnalyzer.Services;

public class ScraperService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private const string URL_BASE = "https://www.resultadoskinochile.com/resultados-kino/page/{0}/";

    public ScraperService(AppDbContext db, HttpClient http)
    {
        _db = db;
        _http = http;
    }

    public async Task<List<string>> ObtenerLinksDePagina(int numeroPagina)
    {
        var url = string.Format(URL_BASE, numeroPagina);
        Console.WriteLine($"Leyendo página {numeroPagina}...");

        var respuesta = await _http.GetAsync(url);

        if (!respuesta.IsSuccessStatusCode)
        {
            Console.WriteLine($"  → página {numeroPagina} no existe, terminamos");
            return new List<string>();
        }

        var html = await respuesta.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode
            .SelectNodes("//a[@class='post-more-link']")
            ?.Select(a => a.GetAttributeValue("href", ""))
            .Where(href => !string.IsNullOrEmpty(href))
            .ToList() ?? new List<string>();

        Console.WriteLine($"  → encontré {links.Count} sorteos");
        return links;
    }

    public async Task<(int numero, string fecha, List<int> numeros)?> ObtenerDatosSorteo(string url)
    {
        var respuesta = await _http.GetAsync(url);
        var html = await respuesta.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tablaKino = doc.DocumentNode.SelectSingleNode("//table[@class='kn-d']");
        if (tablaKino == null) return null;

        var bolitas = tablaKino.SelectNodes(".//span[@class='bola']");
        if (bolitas == null) return null;

        var numeros = bolitas
            .Select(b => int.Parse(b.InnerText.Trim()))
            .ToList();

        var fechaTag = doc.DocumentNode.SelectSingleNode("//time[@class='entry-date']");
        var fecha = fechaTag?.InnerText.Trim() ?? "sin fecha";

        var titulo = doc.DocumentNode.SelectSingleNode("//h1[@class='entry-title']");
        var numeroSorteo = 0;
        if (titulo != null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                titulo.InnerText, @"sorteo\s+(\d+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                numeroSorteo = int.Parse(match.Groups[1].Value);
        }

        return (numeroSorteo, fecha, numeros);
    }

    public async Task<(int guardados, int saltados)> ScraperCompleto()
    {
        int pagina = 1;
        int totalGuardados = 0;
        int totalSaltados = 0;
        int imagenesSeguidadas = 0;

        while (true)
        {
            var links = await ObtenerLinksDePagina(pagina);
            if (!links.Any()) break;

            foreach (var link in links)
            {
                var datos = await ObtenerDatosSorteo(link);

                if (datos == null)
                {
                    imagenesSeguidadas++;
                    Console.WriteLine($"  → formato imagen, saltando ({imagenesSeguidadas} seguidas)");
                    if (imagenesSeguidadas >= 5)
                    {
                        Console.WriteLine("Llegamos al límite histórico, terminamos.");
                        return (totalGuardados, totalSaltados);
                    }
                    totalSaltados++;
                    continue;
                }

                imagenesSeguidadas = 0;

                var existe = _db.Sorteos.Any(s => s.NumeroSorteo == datos.Value.numero);
                if (existe)
                {
                    Console.WriteLine($"  → sorteo {datos.Value.numero} ya existe, terminamos");
                    return (totalGuardados, totalSaltados);
                }

                var sorteo = new Sorteo
                {
                    NumeroSorteo = datos.Value.numero,
                    FechaSorteo = DateTime.Parse(datos.Value.fecha),
                    RastreadoEn = DateTime.Now,
                    UrlFuente = link,
                    Numeros = datos.Value.numeros.Select((n, i) => new NumeroSorteado
                    {
                        Numero = n,
                        Posicion = i + 1
                    }).ToList()
                };

                _db.Sorteos.Add(sorteo);
                await _db.SaveChangesAsync();

                Console.WriteLine($"  ✓ guardado sorteo {datos.Value.numero} ({datos.Value.fecha})");
                totalGuardados++;

                await Task.Delay(500);
            }

            pagina++;
            await Task.Delay(1000);
        }

        return (totalGuardados, totalSaltados);
    }
}