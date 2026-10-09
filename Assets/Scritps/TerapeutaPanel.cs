using System.Collections;
using ReportesPrueba;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class TerapeutaPanel : MonoBehaviour
{
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // Interruptor para pruebas: marcado = lee Resources/sesiones_prueba.json (sin API).
    // Desmarcado = usa la API.
    [SerializeField] private bool usarJsonPrueba = false;

    // Elementos del panel
    private ScrollView listaPacientes;
    private Button btnAbrirRegistro;
    private Button btnCerrarSesion;
    private VisualElement formRegistro;
    private Button btnCerrarFormX; // <--- NUEVA VARIABLE
    private Label mensajeLabel;
    private Button btnGuardarPrueba;//solo de prueba
    private VisualElement panelReportes;
    private PacienteData pacienteSeleccionado;

    // ---- Informe de evolución (gráfica por día + tabla) ----
    private ReportesPaciente reportes;

    // ---- Paciente seleccionado en la lista ----
    private VisualElement itemSeleccionado;
    private static readonly Color ColorItem = new Color(0.35f, 0.35f, 0.4f);
    private static readonly Color ColorItemActivo = new Color(0.2f, 0.5f, 0.8f);


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
        btnCerrarFormX = root.Q<Button>("btn-cerrar-x"); // <--- NUEVA BÚSQUEDA
        mensajeLabel = root.Q<Label>("reg-mensaje");
        btnGuardarPrueba = root.Q<Button>("btn-guardar-prueba");//solo de prueba
        panelReportes = root.Q<VisualElement>("panel-reportes");

        // El informe se dibuja dentro del ScrollView "reportes-contenido"
        reportes = new ReportesPaciente(root);

        // Al empezar no hay paciente seleccionado: se ve "Selecciona un paciente"
        MostrarDetallePaciente(null);

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
            btnAbrirRegistro.clicked += AbrirModalRegistro;

        // Conectamos la X con el método que cierra la ventana
        if (btnCerrarFormX != null)
            btnCerrarFormX.clicked += CerrarModalRegistro;

        if (btnGuardarRegistro != null)
            btnGuardarRegistro.clicked += RegistrarPaciente;

        if (btnCerrarSesion != null)
            btnCerrarSesion.clicked += CerrarSesion;

        if (btnGuardarPrueba != null)
            btnGuardarPrueba.clicked += GuardarSesionPrueba;//solo de prueba

        // Conectar botón Ver Reportes
        Button btnVerReportes = root.Q<Button>("btn-ver-reportes");
        if (btnVerReportes != null)
        {
            btnVerReportes.clicked += () => {
                if (usarJsonPrueba)
                {
                    // Modo prueba: lee el JSON local, no necesita paciente ni API
                    if (panelReportes != null)
                        panelReportes.style.display = DisplayStyle.Flex;

                    CargarSesionesDePrueba();
                }
                else if (pacienteSeleccionado != null)
                {
                    // Mostrar el panel de reportes
                    if (panelReportes != null)
                        panelReportes.style.display = DisplayStyle.Flex;

                    // Cargar las sesiones del paciente
                    CargarSesionesPaciente(pacienteSeleccionado.id);
                }
                else
                {
                    MostrarMensajeRegistro("Selecciona un paciente primero", false);
                }
            };
        }

        Button btnCerrarReportes = root.Q<Button>("btn-cerrar-reportes");
        if (btnCerrarReportes != null)
            btnCerrarReportes.clicked += () => {
                if (panelReportes != null)
                    panelReportes.style.display = DisplayStyle.None;
            };

        // Aseguramos que empiece oculto
        AlternarModal(formRegistro, false);
    }

    private void OnDisable()
    {
        // Desconectar eventos (buena práctica)
        if (btnAbrirRegistro != null)
            btnAbrirRegistro.clicked -= AbrirModalRegistro;

        if (btnCerrarFormX != null)
            btnCerrarFormX.clicked -= CerrarModalRegistro; // <--- NUEVA DESCONEXIÓN

        if (btnGuardarRegistro != null)
            btnGuardarRegistro.clicked -= RegistrarPaciente;

        if (btnCerrarSesion != null)
            btnCerrarSesion.clicked -= CerrarSesion;

        if (btnGuardarPrueba != null)
            btnGuardarPrueba.clicked -= GuardarSesionPrueba;//solo de prueba
    }

    private void AbrirModalRegistro() => AlternarModal(formRegistro, true);
    private void CerrarModalRegistro() => AlternarModal(formRegistro, false);

    // Método genérico y ultra escalable para tus futuras ventanas emergentes
    private void AlternarModal(VisualElement modal, bool mostrar)
    {
        if (modal != null)
        {
            modal.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
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

                // La lista se reconstruye: ya no hay paciente seleccionado
                itemSeleccionado = null;
                pacienteSeleccionado = null;
                MostrarDetallePaciente(null);

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

        // Limpiar la selección de paciente
        pacienteSeleccionado = null;
        itemSeleccionado = null;
        MostrarDetallePaciente(null);

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
        contenedor.style.backgroundColor = ColorItem;
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

        btnProgreso.clicked += () =>
        {
            Debug.Log($"Ver progreso del paciente: {paciente.nombre} (ID: {paciente.id})");
        };

        contenedor.Add(infoLabel);
        contenedor.Add(btnProgreso);

        // Al hacer clic en el paciente se selecciona y se muestra su detalle
        contenedor.RegisterCallback<ClickEvent>(evt => SeleccionarPaciente(paciente, contenedor));

        return contenedor;
    }

    // ============================================
    // SELECCIÓN DE PACIENTE Y PANEL DE DETALLE
    // ============================================
    private void SeleccionarPaciente(PacienteData paciente, VisualElement item)
    {
        pacienteSeleccionado = paciente;

        // Quitar el resaltado del anterior y resaltar el nuevo
        if (itemSeleccionado != null)
            itemSeleccionado.style.backgroundColor = ColorItem;

        itemSeleccionado = item;
        item.style.backgroundColor = ColorItemActivo;

        MostrarDetallePaciente(paciente);
        Debug.Log($"Paciente seleccionado: {paciente.nombre} (ID: {paciente.id})");
    }

    // paciente == null  -> muestra "Selecciona un paciente"
    // paciente != null  -> muestra el detalle con sus botones
    private void MostrarDetallePaciente(PacienteData paciente)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var detalleVacio = root.Q<VisualElement>("detalle-vacio");
        var detallePaciente = root.Q<VisualElement>("detalle-paciente");
        var lblNombre = root.Q<Label>("lbl-paciente-nombre");

        bool hayPaciente = paciente != null;

        if (detalleVacio != null)
            detalleVacio.style.display = hayPaciente ? DisplayStyle.None : DisplayStyle.Flex;

        if (detallePaciente != null)
            detallePaciente.style.display = hayPaciente ? DisplayStyle.Flex : DisplayStyle.None;

        if (hayPaciente && lblNombre != null)
            lblNombre.text = $"{paciente.nombre} · {paciente.cedula} · {paciente.edad} años";
    }

    // ============================================
    // GUARDAR SESIÓN DE PRUEBA (simula datos de terapia)
    // ============================================
    private void GuardarSesionPrueba()
    {
        // Verificar que hay un terapeuta logueado
        if (LoginManager.UsuarioActual == null)
        {
            MostrarMensajeRegistro("Primero inicia sesión como terapeuta", false);
            return;
        }

        // Usar un paciente fijo para pruebas (cambia el ID por uno real de tu MongoDB)
        string idPacienteFijo = "6a120a7e3323054e9a659271";

        string[] nombresTerapia = { "Alcance", "Trayectoria", "Secuencias" };
        string[] niveles = { "baja", "media", "alta" };

        int idTerapia = Random.Range(1, 4);              // 1, 2 o 3
        int intentos = Random.Range(12, 25);
        int aciertos = Random.Range(intentos / 2, intentos + 1);
        bool completada = Random.value > 0.1f;

        // Crear datos de prueba
        SesionRequest sesionPrueba = new SesionRequest
        {
            IdPaciente = idPacienteFijo,
            IdTerapeuta = LoginManager.UsuarioActual.id,
            IdTerapia = idTerapia,
            NombreTerapia = nombresTerapia[idTerapia - 1],
            Puntaje = Mathf.RoundToInt(100f * aciertos / intentos),
            Precision = Mathf.Round(1000f * aciertos / intentos) / 10f,
            TiempoSegundos = Random.Range(30, 90),
            Fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), // la API la sobrescribe
            Aciertos = aciertos,
            Errores = intentos - aciertos,
            Dificultad = niveles[Random.Range(0, 3)],
            Completada = completada,
            Notas = completada ? "" : "Abandonó (prueba)"
        };

        string jsonData = JsonUtility.ToJson(sesionPrueba);
        StartCoroutine(EnviarSesionPrueba(jsonData));
    }

    private IEnumerator EnviarSesionPrueba(string jsonData)
    {
        string url = "http://localhost:5276/api/Sesiones";  // Asegúrate de que sea /api/Sesiones

        MostrarMensajeRegistro("Guardando sesión de prueba...", true);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("data:" + jsonData);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MostrarMensajeRegistro("Sesión de prueba guardada correctamente", true);
                Debug.Log("Sesión guardada: " + request.downloadHandler.text);
            }
            else
            {
                MostrarMensajeRegistro($"Error: {request.error}", false);
                Debug.LogError($"Error al guardar sesión: {request.error}");
            }
        }
    }

    // ============================================
    // MOSTRAR REPORTE (gráfica por día + tabla)
    // ============================================
    private void DibujarGrafica(SesionData[] sesiones)
    {
        if (reportes == null)
            reportes = new ReportesPaciente(GetComponent<UIDocument>().rootVisualElement);

        if (sesiones == null || sesiones.Length == 0)
        {
            MostrarMensajeRegistro("No hay sesiones para mostrar", false);
            return;
        }

        reportes.Mostrar(sesiones);
    }

    // ============================================
    // CARGAR SESIONES DE PRUEBA DESDE Assets/Resources/sesiones_prueba.json
    // ============================================
    private void CargarSesionesDePrueba()
    {
        TextAsset archivo = Resources.Load<TextAsset>("sesiones_prueba");
        if (archivo == null)
        {
            Debug.LogError("No se encontró Assets/Resources/sesiones_prueba.json");
            MostrarMensajeRegistro("Falta Resources/sesiones_prueba.json", false);
            return;
        }

        SesionesWrapper wrapper = JsonUtility.FromJson<SesionesWrapper>(archivo.text);
        DibujarGrafica(wrapper != null ? wrapper.sesiones : null);
    }

    // ============================================
    // CARGAR SESIONES DESDE LA API
    // ============================================
    public void CargarSesionesPaciente(string idPaciente)
    {
        if (string.IsNullOrEmpty(idPaciente))
        {
            MostrarMensajeRegistro("Selecciona un paciente", false);
            return;
        }

        string url = $"http://localhost:5276/api/Sesiones/paciente/{idPaciente}";
        StartCoroutine(GetSesiones(url));
    }

    private IEnumerator GetSesiones(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"Sesiones recibidas: {json}");

                if (json == "[]" || string.IsNullOrEmpty(json))
                {
                    MostrarMensajeRegistro("No hay sesiones para este paciente", false);
                    yield break;
                }

                string jsonArray = "{\"sesiones\":" + json + "}";
                SesionesWrapper wrapper = JsonUtility.FromJson<SesionesWrapper>(jsonArray);

                if (wrapper.sesiones != null && wrapper.sesiones.Length > 0)
                {
                    DibujarGrafica(wrapper.sesiones);
                }
                else
                {
                    MostrarMensajeRegistro("No hay sesiones para mostrar", false);
                }
            }
            else
            {
                MostrarMensajeRegistro($"Error: {request.error}", false);
            }
        }
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

// ============================================
// CLASE PARA RECIBIR DATOS DE SESIONES DESDE LA API
// Es el contrato: mismos nombres que el documento de Mongo y que la clase Sesion de la API.
// ============================================
[System.Serializable]
public class SesionData
{
    public string id;
    public string idPaciente;
    public string idTerapeuta;
    public int idTerapia;
    public string nombreTerapia;
    public int puntaje;
    public float precision;
    public int tiempoSegundos;
    public string fecha;

    // Métricas para el informe detallado
    public int aciertos;
    public int errores;
    public string dificultad;
    public bool completada;
    public string notas;
}

[System.Serializable]
public class SesionesWrapper
{
    public SesionData[] sesiones;
}
