using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;

public class LoginPanel : MonoBehaviour
{
    // ============================================
    // URL DE LA API (cambia el puerto si es necesario)
    // ============================================
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // ============================================
    // REFERENCIAS A LA UI
    // ============================================
    private TextField cedulaInput;
    private TextField contraseñaInput;
    private Button loginBoton;
    private Label mensajeLabel;

    // ============================================
    // SE EJECUTA AL ACTIVAR EL PANEL
    // ============================================
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Buscar elementos por su nombre
        cedulaInput = root.Q<TextField>("login-usuario");
        contraseñaInput = root.Q<TextField>("login-password");
        loginBoton = root.Q<Button>("login-boton");
        mensajeLabel = root.Q<Label>("mensaje-login");

        // Configurar campo de contraseña (oculta el texto)
        if (contraseñaInput != null)
            contraseñaInput.isPasswordField = true;

        // Conectar el botón al método de inicio de sesión
        if (loginBoton != null)
            loginBoton.clicked += IniciarSesion;
    }

    // ============================================
    // SE EJECUTA AL DESACTIVAR EL PANEL
    // ============================================
    private void OnDisable()
    {
        if (loginBoton != null)
            loginBoton.clicked -= IniciarSesion;
    }

    // ============================================
    // VALIDAR CAMPOS Y LLAMAR AL LOGIN
    // ============================================
    private void IniciarSesion()
    {
        string cedula = cedulaInput?.value;
        string contraseña = contraseñaInput?.value;

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

    // ============================================
    // PETICIÓN A LA API PARA VALIDAR CREDENCIALES
    // ============================================
    private IEnumerator LoginRequest(string cedula, string contraseña)
    {
        string url = $"{apiUrl}/login/{cedula}?contrasena={contraseña}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            // ========================================
            // CASO 1: LOGIN EXITOSO (200)
            // ========================================
            if (request.result == UnityWebRequest.Result.Success)
            {
                UsuarioData usuario = JsonUtility.FromJson<UsuarioData>(request.downloadHandler.text);

                // Solo terapeutas pueden iniciar sesión
                if (usuario.rol != "terapeuta")
                {
                    MostrarMensaje("Acceso no autorizado", false);
                    LimpiarCampos();
                    yield break;
                }

                LoginManager.UsuarioActual = usuario;
                MostrarMensaje($"Bienvenido {usuario.nombre}", true);
                LimpiarCampos();
                ActivarPanelTerapeuta();
            }
            // ========================================
            // CASO 2: CONTRASEÑA INCORRECTA (401)
            // ========================================
            else if (request.responseCode == 401)
            {
                MostrarMensaje("Contraseña incorrecta", false);
                LimpiarCampos();
            }
            // ========================================
            // CASO 3: USUARIO NO EXISTE (404)
            // ========================================
            else if (request.responseCode == 404)
            {
                MostrarMensaje("Cédula no registrada", false);
                LimpiarCampos();
            }
            // ========================================
            // CASO 4: ERROR DE CONEXIÓN
            // ========================================
            else
            {
                MostrarMensaje("Error de conexión", false);
            }
        }
    }

    // ============================================
    // MOSTRAR MENSAJE EN LA UI
    // ============================================
    private void MostrarMensaje(string mensaje, bool esExito)
    {
        if (mensajeLabel == null) return;

        mensajeLabel.text = mensaje;
        mensajeLabel.style.color = esExito ? Color.green : Color.red;
    }

    // ============================================
    // ACTIVAR PANEL DEL TERAPEUTA
    // ============================================
    private void ActivarPanelTerapeuta()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var loginContainer = root.Q<VisualElement>("login");
        var contenidoTerapeuta = root.Q<VisualElement>("contenido-terapeuta");

        if (loginContainer != null)
            loginContainer.style.display = DisplayStyle.None;

        if (contenidoTerapeuta != null)
            contenidoTerapeuta.style.display = DisplayStyle.Flex;

        var terapeutaPanel = GetComponent<TerapeutaPanel>();
        if (terapeutaPanel != null)
            terapeutaPanel.CargarListaPacientes();
    }

    // ============================================
    // LIMPIAR CAMPOS DEL FORMULARIO
    // ============================================
    private void LimpiarCampos()
    {
        if (cedulaInput != null) cedulaInput.value = "";
        if (contraseñaInput != null) contraseñaInput.value = "";
    }
}

// ============================================
// CLASE PARA RECIBIR DATOS DEL USUARIO DESDE LA API
// ============================================
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