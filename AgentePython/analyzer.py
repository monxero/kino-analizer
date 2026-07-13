import sqlite3

RUTA_BD = "../Data/kino.db"


def obtener_ultimos_sorteos(n: int = 10):
    """Devuelve los últimos N sorteos con sus números, del más reciente al más antiguo."""
    with sqlite3.connect(RUTA_BD) as conexion:
        cursor = conexion.cursor()

        cursor.execute("""
            SELECT Id, NumeroSorteo, FechaSorteo
            FROM Sorteos
            ORDER BY FechaSorteo DESC
            LIMIT ?
        """, (n,))
        sorteos = cursor.fetchall()

        resultado = []
        for sorteo_id, numero_sorteo, fecha in sorteos:
            cursor.execute("""
                SELECT Numero
                FROM NumerosSorteados
                WHERE SorteoId = ?
                ORDER BY Posicion
            """, (sorteo_id,))
            numeros = [fila[0] for fila in cursor.fetchall()]

            resultado.append({
                "numero_sorteo": numero_sorteo,
                "fecha": fecha,
                "numeros": numeros
            })

    return resultado

def frecuencia_numeros():
    """Devuelve cuántas veces salió cada número en todo el historial."""
    with sqlite3.connect(RUTA_BD) as conexion:
        cursor = conexion.cursor()
        cursor.execute("""
            SELECT Numero, COUNT(*) as veces
            FROM NumerosSorteados
            GROUP BY Numero
            ORDER BY veces DESC
        """)
        filas = cursor.fetchall()

    return {numero: veces for numero, veces in filas}


def buscar_numero_en_sorteos(numero: int, cantidad_sorteos: int):
    """Busca en cuáles de los últimos N sorteos apareció un número específico."""
    with sqlite3.connect(RUTA_BD) as conexion:
        cursor = conexion.cursor()
        cursor.execute("""
            SELECT s.NumeroSorteo, s.FechaSorteo
            FROM Sorteos s
            JOIN NumerosSorteados n ON n.SorteoId = s.Id
            WHERE n.Numero = ?
              AND s.Id IN (
                  SELECT Id FROM Sorteos
                  ORDER BY FechaSorteo DESC
                  LIMIT ?
              )
            ORDER BY s.FechaSorteo DESC
        """, (numero, cantidad_sorteos))
        filas = cursor.fetchall()

    return [{"numero_sorteo": ns, "fecha": fecha} for ns, fecha in filas]