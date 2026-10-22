using System;
using UnityEngine;

// ============================================================
// CONTRATO DE TERAPIAS
// Estas clases tienen los mismos nombres que Resources/terapias.json.
// Hoy el catálogo se lee de ese archivo; más adelante puede venir
// de MongoDB a través de la API con exactamente el mismo formato.
// ============================================================

[Serializable]
public class NivelesDef
{
    public float baja;
    public float media;
    public float alta;

    public float Obtener(string nivel)
    {
        switch (nivel)
        {
            case "baja": return baja;
            case "alta": return alta;
            default: return media;
        }
    }
}

[Serializable]
public class VariableDef
{
    public string clave;       // nombre interno; es lo que lee la escena
    public string etiqueta;    // texto que ve el terapeuta
    public string unidad;      // "cm", "s", "%", ""...
    public float min;
    public float max;
    public float paso = 1f;
    public NivelesDef porNivel; // valor que se carga al elegir baja, media o alta
}

[Serializable]
public class TerapiaDef
{
    public int id;             // el mismo idTerapia que guardan las sesiones
    public string nombre;      // el mismo nombreTerapia que guardan las sesiones
    public string objetivo;
    public string escena;      // nombre de la escena de Unity (debe estar en Build Settings)
    public VariableDef[] variables;
}

[Serializable]
public class CatalogoTerapias
{
    public TerapiaDef[] terapias;

    public static CatalogoTerapias Cargar()
    {
        TextAsset archivo = Resources.Load<TextAsset>("terapias");
        if (archivo == null)
        {
            Debug.LogError("No se encontró Assets/Resources/terapias.json");
            return new CatalogoTerapias { terapias = new TerapiaDef[0] };
        }

        CatalogoTerapias catalogo = JsonUtility.FromJson<CatalogoTerapias>(archivo.text);
        if (catalogo == null || catalogo.terapias == null)
        {
            Debug.LogError("terapias.json no tiene el formato esperado: { \"terapias\": [ ... ] }");
            return new CatalogoTerapias { terapias = new TerapiaDef[0] };
        }
        return catalogo;
    }
}
