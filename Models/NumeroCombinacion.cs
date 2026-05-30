namespace KinoAnalyzer.Models;

public class NumeroCombinacion
{
    public int Id { get; set; }
    public int CombinacionId { get; set; }
    public int Numero { get; set; }

    // Navegación inversa
    public CombinacionUsuario Combinacion { get; set; } = null!;
}