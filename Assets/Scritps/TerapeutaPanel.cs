using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;

public class TerapeutaPanel : MonoBehaviour
{
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // Elementos del panel
    private ScrollView listaPacientes;
    private Button btnAbrirRegistro;
    private Button btnCerrarSesion;
    private VisualElement formRegistro;
    private Label mensajeLabel;

    // Campos del formulario
    private TextField regCedula;
    private TextField regNombre;
    private TextField regEmail;
    private IntegerField regEdad;
    private TextField regContraseña;
    private Button btnGuardarRegistro;
    private Label regMensaje;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Buscar elementos del panel terapeuta
        listaPacientes = root.Q<ScrollView>("lista-pacientes");
        btnAbrirRegistro = root.Q<Button>("btn-abrir-registro");
        btnCerrarSesion = root.Q<Button>("btn-cerrar-sesion-terapeuta");
        formRegistro = root.Q<VisualElement>("form-registro");
        mensajeLabel = root.Q<Label>("reg-mensaje");

        // Campos del formulario de registro
        regCedula = root.Q<TextField>("reg-cedula");
        regNombre = root.Q<TextField>("reg-nombre");
        regEmail = root.Q<TextField>("reg-email");
        regEdad = root.Q<IntegerField>("reg-edad");
        regContraseña = root.Q<TextField>("reg-contrasena");
        btnGuardarRegistro = root.Q<Button>("btn-guardar-registro");

        // Configurar campo de contraseña
        if (regContraseña != null)
        {
            regContraseña.isPasswordField = true;
        }

        // Conectar eventos
        if (btnAbrirRegistro != null)
            btnAbrirRegistro.clicked += () => MostrarFormularioRegistro(true);

        if (btnGuardarRegistro != null)
            btnGuardarRegistro.clicked += RegistrarPaciente;

        if (btnCerrarSesion != null)
            btnCerrarSesion.clicked += CerrarSesion;

        // Ocultar formulario al inicio
        MostrarFormularioRegistro(false);
    }

    private void OnDisable()
    {
        // Desconectar eventos (buena práctica)
        if (btnAbrirRegistro != null)
            btnAbrirRegistro.clicked -= () => MostrarFormularioRegistro(true);

        if (btnGuardarRegistro != null)
            btnGuardarRegistro.clicked -= RegistrarPaciente;

        if (btnCerrarSesion != null)
            btnCerrarSesion.clicked -= CerrarSesion;
    }

    private void MostrarFormularioRegistro(bool mostrar)
    {
        if (formRegistro != null)
        {
            formRegistro.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void CargarListaPacientes()
    {
        if (LoginManager.UsuarioActual == null)
        {
            Debug.LogError("No hay terapeuta logueado");
            return;
        }

        string idTerapeuta = LoginManager.UsuarioActual.id;
        StartCoroutine(GetPacientes(idTerapeuta));
    }

    private IEnumerator GetPacientes(string idTerapeuta)
    {
        string url = $"{apiUrl}/mis-pacientes/{idTerapeuta}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string respuesta = request.downloadHandler.text;
                Debug.Log($"Pacientes recibidos: {respuesta}");

                // ============================================
                // NOTA: Limpiar la lista actual
                // ============================================
                listaPacientes.Clear();

                // ============================================
                // NOTA: Convertir el JSON array a un objeto usable
                // La API devuelve: [{...}, {...}, ...]
                // Lo envolvemos en un objeto con propiedad "pacientes"
                // ============================================
                string jsonArray = "{\"pacientes\":" + respuesta + "}";
                PacienteListWrapper wrapper = JsonUtility.FromJson<PacienteListWrapper>(jsonArray);

                // ============================================
                // NOTA: Verificar si hay pacientes
                // ============================================
                if (wrapper.pacientes == null || wrapper.pacientes.Length == 0)
                {
                    var label = new Label("No hay pacientes registrados aún.\nPresiona 'Registrar Paciente' para agregar.");
                    label.style.color = Color.gray;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    label.style.paddingTop = 20;
                    listaPacientes.Add(label);
                    yield break;
                }

                // ============================================
                // NOTA: Crear un elemento visual por cada paciente
                // ============================================
                foreach (var paciente in wrapper.pacientes)
                {
                    VisualElement contenedor = CrearElementoPaciente(paciente);
                    listaPacientes.Add(contenedor);
                }

                Debug.Log($"Mostrando {wrapper.pacientes.Length} pacientes en la lista");
            }
            else
            {
                Debug.LogError($"Error al cargar pacientes: {request.error}");
                var label = new Label($"Error de conexión: {request.error}");
                label.style.color = Color.red;
                listaPacientes.Add(label);
            }
        }
    }

    private void RegistrarPaciente()
    {
        // Validar campos obligatorios
        if (string.IsNullOrEmpty(regCedula?.value))
        {
            MostrarMensajeRegistro("Ingresa la cédula", false);
            return;
        }

        if (string.IsNullOrEmpty(regNombre?.value))
        {
            MostrarMensajeRegistro("Ingresa el nombre", false);
            return;
        }

        if (string.IsNullOrEmpty(regContraseña?.value))
        {
            MostrarMensajeRegistro("Ingresa la contraseña", false);
            return;
        }

        if (LoginManager.UsuarioActual == null)
        {
            MostrarMensajeRegistro("No hay terapeuta logueado", false);
            return;
        }

        // Crear objeto con los datos del paciente
        PacienteRegistro nuevoPaciente = new PacienteRegistro
        {
            cedula = regCedula.value,
            nombre = regNombre.value,
            email = regEmail?.value ?? "",
            edad = regEdad?.value ?? 0,
            contraseña = regContraseña.value,
            id_terapeuta = LoginManager.UsuarioActual.id
        };

        string jsonData = JsonUtility.ToJson(nuevoPaciente);
        StartCoroutine(EnviarRegistro(jsonData));
    }

    private IEnumerator EnviarRegistro(string jsonData)
    {
        MostrarMensajeRegistro("Registrando paciente...", true);

        string url = $"{apiUrl}/registro";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MostrarMensajeRegistro("Paciente registrado correctamente", true);
                LimpiarFormulario();
                MostrarFormularioRegistro(false);
                CargarListaPacientes(); // Recargar la lista
            }
            else
            {
                MostrarMensajeRegistro($"Error: {request.error}", false);
                Debug.LogError($"Error en registro: {request.error}");
            }
        }
    }

    private void MostrarMensajeRegistro(string mensaje, bool esExito)
    {
        if (mensajeLabel != null)
        {
            mensajeLabel.text = mensaje;
            mensajeLabel.style.color = esExito ? Color.green : Color.red;
        }
    }

    private void LimpiarFormulario()
    {
        if (regCedula != null) regCedula.value = "";
        if (regNombre != null) regNombre.value = "";
        if (regEmail != null) regEmail.value = "";
        if (regEdad != null) regEdad.value = 0;
        if (regContraseña != null) regContraseña.value = "";
    }

    private void CerrarSesion()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Buscar paneles
        var loginContainer = root.Q<VisualElement>("login");
        var contenidoTerapeuta = root.Q<VisualElement>("contenido-terapeuta");

        // Ocultar panel terapeuta
        if (contenidoTerapeuta != null)
            contenidoTerapeuta.style.display = DisplayStyle.None;

        // Mostrar login
        if (loginContainer != null)
            loginContainer.style.display = DisplayStyle.Flex;

        // Limpiar usuario logueado
        LoginManager.UsuarioActual = null;

        // Limpiar campos de login (opcional)
        var cedulaInput = root.Q<TextField>("login-usuario");
        var passInput = root.Q<TextField>("login-password");
        if (cedulaInput != null) cedulaInput.value = "";
        if (passInput != null) passInput.value = "";

        // Limpiar mensaje de login
        var mensajeLogin = root.Q<Label>("mensaje-login");
        if (mensajeLogin != null) mensajeLogin.text = "";
    }

    // ============================================
    // CREAR ELEMENTO VISUAL PARA CADA PACIENTE
    // ============================================
    private VisualElement CrearElementoPaciente(PacienteData paciente)
    {
        var contenedor = new VisualElement();
        contenedor.style.marginBottom = 10;
        contenedor.style.paddingLeft = 8;
        contenedor.style.paddingRight = 8;
        contenedor.style.paddingTop = 8;
        contenedor.style.paddingBottom = 8;
        contenedor.style.backgroundColor = new Color(0.35f, 0.35f, 0.4f);
        contenedor.style.borderTopLeftRadius = 8;
        contenedor.style.borderTopRightRadius = 8;
        contenedor.style.borderBottomLeftRadius = 8;
        contenedor.style.borderBottomRightRadius = 8;
        contenedor.style.flexDirection = FlexDirection.Row;
        contenedor.style.alignItems = Align.Center;
        contenedor.style.justifyContent = Justify.SpaceBetween;

        var infoLabel = new Label($"{paciente.nombre}\n   {paciente.cedula} | {paciente.edad} años");
        infoLabel.style.color = Color.white;
        infoLabel.style.fontSize = 14;

        var btnProgreso = new Button();
        btnProgreso.text = "Ver progreso";
        btnProgreso.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
        btnProgreso.style.color = Color.white;
        btnProgreso.style.paddingLeft = 10;
        btnProgreso.style.paddingRight = 10;
        btnProgreso.style.paddingTop = 5;
        btnProgreso.style.paddingBottom = 5;
        btnProgreso.style.borderTopLeftRadius = 5;
        btnProgreso.style.borderTopRightRadius = 5;
        btnProgreso.style.borderBottomLeftRadius = 5;
        btnProgreso.style.borderBottomRightRadius = 5;

        btnProgreso.clicked += () => {
            Debug.Log($"Ver progreso del paciente: {paciente.nombre} (ID: {paciente.id})");
        };

        contenedor.Add(infoLabel);
        contenedor.Add(btnProgreso);

        return contenedor;
    } 
}

// Clase para enviar el registro del paciente POST
[System.Serializable]
public class PacienteRegistro
{
    public string cedula;
    public string nombre;
    public string email;
    public int edad;
    public string contraseña;
    public string id_terapeuta;
}

// ============================================
// CLASE PARA RECIBIR DATOS DEL PACIENTE DESDE LA API (GET)
// ============================================
[System.Serializable]
public class PacienteData
{
    public string id;           // ID en MongoDB (lo genera la BD)
    public string cedula;       // Cédula del paciente
    public string nombre;       // Nombre completo
    public string email;        // Correo electrónico
    public int edad;            // Edad
    public string rol;          // "paciente"
    public string id_terapeuta; // ID del terapeuta que lo creó
}

// ============================================
// CLASE AYUDANTE PARA PARSEAR EL ARRAY DE PACIENTES
// ============================================
[System.Serializable]
public class PacienteListWrapper
{
    public PacienteData[] pacientes;
}