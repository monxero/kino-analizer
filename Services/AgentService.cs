using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KinoAnalyzer.Data;
using Microsoft.Extensions.Configuration;

namespace KinoAnalyzer.Services;

public class MensajeChat
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AgentService
{
    private readonly KinoStatsService _stats;
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public AgentService(KinoStatsService stats, HttpClient http, IConfiguration config)
    {
        _stats = stats;
        _http = http;
        _apiKey = config["Gemini:ApiKey"] ?? throw new Exception("Gemini API key no configurada");
    }

    public async Task<string> Preguntar(string pregunta, List<MensajeChat> historial)
    {
        var total = await _stats.ObtenerTotalSorteos();

        var systemPrompt = $"""
            Eres un experto en estadísticas del Kino de Chile.
            Tienes acceso a datos históricos de {total} sorteos.
            Responde en español, sé claro y directo.
            Solo respondes preguntas sobre estadísticas del Kino.
            Si no puedes responder algo con los datos disponibles, 
            dilo claramente y sugiere qué función se podría construir.
            Si te preguntan sobre predicciones, recuerda que el Kino es aleatorio.
            """;

        // Recopilamos contexto estadístico relevante
        var frecuencias = await _stats.ObtenerFrecuencias();
        var atrasados = await _stats.ObtenerAtrasados();
        var distribucion = await _stats.ObtenerDistribucionRangos();

        var contexto = $"""
            DATOS ACTUALES ({total} sorteos analizados):
            - Top 5 frecuentes: {string.Join(", ", frecuencias.Take(5).Select(f => $"{f.Numero}({f.Veces}v)"))}
            - Top 5 atrasados: {string.Join(", ", atrasados.Take(5).Select(a => $"{a.Numero}({a.SorteosSinSalir}s)"))}
            """;

        // Construimos los mensajes para Gemini
        var mensajes = new List<object>();

        // Historial previo
        foreach (var msg in historial)
        {
            mensajes.Add(new
            {
                role = msg.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = msg.Content } }
            });
        }

        // Pregunta actual con contexto
        mensajes.Add(new
        {
            role = "user",
            parts = new[] { new { text = $"{contexto}\n\nPREGUNTA: {pregunta}" } }
        });

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = mensajes
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
        var response = await _http.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Gemini response: {responseJson}");

        var doc = JsonDocument.Parse(responseJson);

        // Si hay error de la API, mostramos el mensaje
        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            var errorMsg = error.GetProperty("message").GetString();
            return $"Error de API: {errorMsg}";
        }

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "No pude generar una respuesta.";
    }
}