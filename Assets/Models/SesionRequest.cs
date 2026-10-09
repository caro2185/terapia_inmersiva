[System.Serializable]
public class SesionRequest
{
    public string IdPaciente;
    public string IdTerapeuta;
    public int IdTerapia;
    public string NombreTerapia;
    public int Puntaje;
    public float Precision;
    public int TiempoSegundos;
    public string Fecha;
    public int Aciertos;
    public int Errores;
    public string Dificultad;
    public bool Completada = true;
    public string Notas;
}