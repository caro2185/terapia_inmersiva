using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;

public class LoginPanel : MonoBehaviour
{
    // URL de tu API (cambia el puerto si es necesario)
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // Referencias a los elementos de la UI
    private TextField cedulaInput;
    private TextField contraseñaInput;
    private Button loginBoton;
    private Label mensajeLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Buscar los elementos por su nombre
        cedulaInput = root.Q<TextField>("login-usuario");
        contraseñaInput = root.Q<TextField>("login-password");
        loginBoton = root.Q<Button>("login-boton");
        mensajeLabel = root.Q<Label>("mensaje-login");

        // Configurar campo de contraseña
        if (contraseñaInput != null)
        {
            contraseñaInput.isPasswordField = true;
        }

        // Conectar el botón
        if (loginBoton != null)
        {
            loginBoton.clicked += IniciarSesion;
        }
    }

    private void OnDisable()
    {
        if (loginBoton != null)
        {
            loginBoton.clicked -= IniciarSesion;
        }
    }

    private void IniciarSesion()
    {
        // Obtener valores
        string cedula = cedulaInput?.value;
        string contraseña = contraseñaInput?.value;

        // Validaciones
        if (string.IsNullOrEmpty(cedula))
        {
            MostrarMensaje("Ingresa la cédula", false);
            return;
        }

        if (string.IsNullOrEmpty(contraseña))
        {
            MostrarMensaje("Ingresa la contraseña", false);
            return;
        }

        MostrarMensaje("Verificando...", true);
        StartCoroutine(LoginRequest(cedula, contraseña));
    }

    private IEnumerator LoginRequest(string cedula, string contraseña)
    {
        // Endpoint de login por cédula
        string url = $"{apiUrl}/login/{cedula}?contrasena={contraseña}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            // Éxito (200)
            if (request.result == UnityWebRequest.Result.Success)
            {
                string respuesta = request.downloadHandler.text;
                Debug.Log($"Login exitoso: {respuesta}");

                // Parsear respuesta
                UsuarioData usuario = JsonUtility.FromJson<UsuarioData>(respuesta);

                // Guardar usuario logueado
                LoginManager.UsuarioActual = usuario;

                // Mostrar mensaje de bienvenida con el nombre real
                MostrarMensaje($"¡Bienvenido {usuario.nombre}!", true);

                // Limpiar campos
                cedulaInput.value = "";
                contraseñaInput.value = "";

                // Redirigir según rol
                if (usuario.rol == "terapeuta")
                {
                    Debug.Log("Redirigiendo a panel de TERAPEUTA");
                    // Cargar escena de terapeuta
                }
                else if (usuario.rol == "paciente")
                {
                    Debug.Log(" Redirigiendo a panel de PACIENTE");
                    // Cargar escena de paciente
                }
            }
            // Error 401: Contraseña incorrecta
            else if (request.responseCode == 401)
            {
                MostrarMensaje(" Contraseña incorrecta", false);
                Debug.LogWarning("Login fallido: Contraseña incorrecta");
            }
            // Error 404: Usuario no encontrado
            else if (request.responseCode == 404)
            {
                MostrarMensaje("Cédula no registrada", false);
                Debug.LogWarning("Login fallido: Cédula no existe");
            }
            // Otros errores
            else
            {
                MostrarMensaje(" Error de conexión", false);
                Debug.LogError($"Error en login: {request.error}");
            }
        }
    }

    private void MostrarMensaje(string mensaje, bool esExito)
    {
        if (mensajeLabel != null)
        {
            mensajeLabel.text = mensaje;
            mensajeLabel.style.color = esExito ? Color.green : Color.red;
        }
    }
}

// Clase para recibir datos del usuario
[System.Serializable]
public class UsuarioData
{
    public string id;
    public string cedula;
    public string nombre;
    public string email;
    public int edad;
    public string rol;
    public string id_terapeuta;
}