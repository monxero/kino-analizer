from fastapi import FastAPI
from pydantic import BaseModel

from agent import preguntar_agente

app = FastAPI()


class Turno(BaseModel):
    role: str
    text: str


class PreguntaRequest(BaseModel):
    pregunta: str
    historial: list[Turno] = []


class RespuestaAgente(BaseModel):
    respuesta: str


@app.post("/chat", response_model=RespuestaAgente)
def chat(request: PreguntaRequest):
    historial_dict = [{"role": t.role, "text": t.text} for t in request.historial]
    texto_respuesta = preguntar_agente(request.pregunta, historial_dict)
    return RespuestaAgente(respuesta=texto_respuesta)