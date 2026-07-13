using System.Text;
using System.Text.Json;

namespace KinoAnalyzer.Services;

public class MensajeChat
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AgentService
{
    private readonly HttpClient _http;

    public AgentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> Preguntar(string pregunta, List<MensajeChat> historial)
    {
        var historialParaPython = historial.Select(msg => new
        {
            role = msg.Role == "assistant" ? "model" : "user",
            text = msg.Content
        });

        var requestBody = new
        {
            pregunta,
            historial = historialParaPython
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("http://localhost:8000/chat", content);

        if (!response.IsSuccessStatusCode)
        {
            return "El agente no está disponible en este momento. Intenta de nuevo en unos segundos.";
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseJson);
        var texto = doc.RootElement.GetProperty("respuesta").GetString();

        return texto ?? "No pude generar una respuesta.";
    }
}