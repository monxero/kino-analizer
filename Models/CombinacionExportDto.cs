using System.Xml.Serialization;

namespace KinoAnalyzer.Models;

[XmlRoot("CombinacionKino")]
public class CombinacionExportDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public DateTime CreadoEn { get; set; }

    [XmlArray("Numeros")]
    [XmlArrayItem("Numero")]
    public List<int> Numeros { get; set; } = new();
}