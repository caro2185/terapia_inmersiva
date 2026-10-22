using System.Collections.Generic;

// ============================================================
// CONFIGURACIÓN ACTIVA
// Es lo que la interfaz le entrega a la escena de la terapia.
// La escena solo necesita leer esta clase en Start().
// ============================================================
public static class ConfiguracionActiva
{
    public static string IdPaciente;
    public static string NombrePaciente;
    public static TerapiaDef Terapia;
    public static string Nivel = "media";
    public static Dictionary<string, float> Valores = new Dictionary<string, float>();

    // Nombre de la escena del menú, para que la terapia pueda volver
    public static string EscenaRetorno;

    public static float Valor(string clave, float porDefecto)
    {
        float v;
        return Valores.TryGetValue(clave, out v) ? v : porDefecto;
    }

    public static void Establecer(string idPaciente, string nombrePaciente, TerapiaDef terapia,
                                  string nivel, Dictionary<string, float> valores, string escenaRetorno)
    {
        IdPaciente = idPaciente;
        NombrePaciente = nombrePaciente;
        Terapia = terapia;
        Nivel = nivel;
        Valores = new Dictionary<string, float>(valores);
        EscenaRetorno = escenaRetorno;
    }
}
