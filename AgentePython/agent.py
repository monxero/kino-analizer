import os
from dotenv import load_dotenv
from google import genai
from google.genai import types

from analyzer import obtener_ultimos_sorteos, frecuencia_numeros, buscar_numero_en_sorteos

load_dotenv()

api_key = os.environ.get("GEMINI_API_KEY")
if not api_key:
    raise ValueError("Falta GEMINI_API_KEY en el archivo .env")

client = genai.Client(api_key=api_key)

INSTRUCCIONES_SISTEMA = """
Eres un asistente que analiza estadísticas históricas del Kino de Chile.
Tenés acceso a herramientas que consultan datos reales de sorteos pasados.

Reglas importantes:
- El Kino es un sorteo aleatorio. Los datos históricos NUNCA predicen resultados futuros.
- Presenta la información como patrones históricos, nunca como predicciones o recomendaciones certeras.
- Si el usuario no especifica sobre cuántos sorteos preguntar, pregúntaselo antes de usar buscar_numero_en_sorteos.
- Responde en español, de forma clara y breve.
"""


def preguntar_agente(pregunta: str, historial: list = None) -> str:
    contents = []
    if historial:
        for turno in historial:
            contents.append(
                types.Content(role=turno["role"], parts=[types.Part(text=turno["text"])])
            )
    contents.append(types.Content(role="user", parts=[types.Part(text=pregunta)]))

    respuesta = client.models.generate_content(
        model="gemini-2.5-flash",
        contents=contents,
        config=types.GenerateContentConfig(
            system_instruction=INSTRUCCIONES_SISTEMA,
            tools=[obtener_ultimos_sorteos, frecuencia_numeros, buscar_numero_en_sorteos],
        ),
    )
    return respuesta.text