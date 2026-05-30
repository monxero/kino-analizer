namespace KinoAnalyzer.Models;

public class CombinacionUsuario
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public bool EsFavorita { get; set; }
    public DateTime CreadoEn { get; set; }

    // Navegación
    public List<NumeroCombinacion> Numeros { get; set; } = new();
}