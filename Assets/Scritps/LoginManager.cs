using UnityEngine;

public static class LoginManager
{
    public static UsuarioData UsuarioActual { get; set; }

    public static bool IsLoggedIn => UsuarioActual != null;

    public static bool IsTerapeuta => IsLoggedIn && UsuarioActual.rol == "terapeuta";

    public static bool IsPaciente => IsLoggedIn && UsuarioActual.rol == "paciente";

    public static string GetIdTerapeutaActual()
    {
        if (IsTerapeuta)
            return UsuarioActual.id;

        if (IsPaciente)
            return UsuarioActual.id_terapeuta;

        return null;
    }
}
