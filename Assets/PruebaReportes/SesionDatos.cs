using System;

namespace ReportesPrueba
{
    // Copia propia de las clases de datos, dentro de un namespace.
    // Así no choca con SesionData / SesionesWrapper de tu TerapeutaPanel.cs.

    [Serializable]
    public class SesionData
    {
        public string id;
        public string idPaciente;
        public string idTerapeuta;
        public int idTerapia;
        public string nombreTerapia;
        public int puntaje;
        public float precision;
        public int tiempoSegundos;
        public string fecha;

        public int aciertos;
        public int errores;
        public string dificultad;   // baja / media / alta
        public bool completada;
        public string notas;
    }

    [Serializable]
    public class SesionesWrapper
    {
        public SesionData[] sesiones;
    }
}
