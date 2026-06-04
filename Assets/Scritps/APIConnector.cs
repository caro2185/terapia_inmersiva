using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIConnector : MonoBehaviour
{
    // La dirección de tu API (la que viste en Swagger)
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // Start se llama al inicio del juego
    void Start()
    {
        // Iniciamos la corrutina para llamar a la API
        StartCoroutine(ObtenerUsuarios());
    }

    // Esta corrutina es la que hace la petición HTTP
    IEnumerator ObtenerUsuarios()
    {
        // Creamos la petición GET
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            // Enviamos la petición y esperamos la respuesta
            yield return request.SendWebRequest();

            // Verificamos si hubo error
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Éxito: mostramos los datos en la consola
                Debug.Log(" Datos recibidos de la API:");
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                // Error: mostramos el problema
                Debug.LogError(" Error al llamar a la API: " + request.error);
            }
        }
    }
}