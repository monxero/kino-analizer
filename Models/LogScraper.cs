namespace KinoAnalyzer.Models;

public class LogScraper
{
    public int Id { get; set; }
    public DateTime IniciadoEn { get; set; }
    public DateTime? FinalizadoEn { get; set; }
    public int SorteosGuardados { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
}