using System;
using System.Collections.Generic;

// ============================================
// DATOS PRINCIPALES
// ============================================
[Serializable]
public class DashboardData
{
    public string paciente;
    public string diagnostico;
    public List<TerapiaData> terapias;
}

[Serializable]
public class TerapiaData
{
    public int id;
    public string nombre;
    public string tipo;
    public List<SesionRaw> sesiones;
}

[Serializable]
public class SesionRaw
{
    public string id;
    public string fecha;
    public int puntaje;
    public float precision;
    public int tiempoSegundos;
    public int aciertos;
    public int errores;
    public string dificultad;
    public bool completada;
}

// ============================================
// DATOS AGRUPADOS POR DÍA
// ============================================
public class DiaAgrupado
{
    public DateTime fecha;
    public int numSesiones;
    public float eficaciaPromedio;
    public float precisionPromedio;
    public float puntajePromedio;
    public float tiempoPromedio;
    public int puntajeMin;
    public int puntajeMax;
    public List<SesionRaw> sesionesDelDia;
}
