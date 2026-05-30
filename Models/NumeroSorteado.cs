namespace KinoAnalyzer.Models;

public class NumeroSorteado
{
    public int Id { get; set; }
    public int SorteoId { get; set; }
    public int Numero { get; set; }
    public int Posicion { get; set; }

    // Navegación inversa — EF Core sabe que este número pertenece a un Sorteo
    public Sorteo Sorteo { get; set; } = null!;
}