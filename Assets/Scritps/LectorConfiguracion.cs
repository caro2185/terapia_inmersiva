using UnityEngine;

/// <summary>
/// Script de PRUEBA para una escena de terapia (por ejemplo EscenaAgarre).
/// Pégalo en cualquier objeto de la escena: al darle Play (entrando desde el menú)
/// escribe en la consola la configuración que eligió el terapeuta.
/// Es lo mismo que tendrá que leer la escena real de tu compañero.
/// </summary>
public class LectorConfiguracion : MonoBehaviour
{
    private void Start()
    {
        if (ConfiguracionActiva.Terapia == null)
        {
            Debug.LogWarning("Sin configuración: la escena se abrió directamente, no desde el menú.");
            return;
        }

        Debug.Log("Paciente: " + ConfiguracionActiva.NombrePaciente +
                  " | Terapia: " + ConfiguracionActiva.Terapia.nombre +
                  " | Nivel: " + ConfiguracionActiva.Nivel);

        foreach (var par in ConfiguracionActiva.Valores)
            Debug.Log("   " + par.Key + " = " + par.Value);

        // Ejemplo de uso en una escena real:
        // float duracion = ConfiguracionActiva.Valor("duracionMin", 15f);
        // Para volver al menú: SceneManager.LoadScene(ConfiguracionActiva.EscenaRetorno);
    }
}
