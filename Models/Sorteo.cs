namespace KinoAnalyzer.Models;

public class Sorteo
{
    public int Id { get; set; }
    public int NumeroSorteo { get; set; }
    public DateTime FechaSorteo { get; set; }
    public DateTime RastreadoEn { get; set; }
    public string UrlFuente { get; set; } = string.Empty;

    public List<NumeroSorteado> Numeros { get; set; } = new();
}