using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class TerapeutaPanel : MonoBehaviour
{
    private string apiUrl = "http://localhost:5276/api/Usuarios";

    // Elementos del panel
    private Button btnAbrirRegistro;
    private Button btnCerrarSesion;
    private VisualElement formRegistro;
    private Button btnCerrarFormX;
    private Label mensajeLabel;
    private Button btnGuardarPrueba;
    private VisualElement panelReportes;
    private PacienteData pacienteSeleccionado;

    // Variables para la búsqueda
    private TextField inputBusqueda;
    private Button btnBuscar;
    private VisualElement resultadoPaciente;
    private Label lblResultadoNombre;
    private Label lblResultadoEdad;
    private Label lblResultadoSesion;
    private List<PacienteData> todosLosPacientes = new List<PacienteData>();

    // ---- Reportes ----
    private ReportesPaciente reportes;
    private ConfigTerapias configTerapias;
    private bool usarJsonPrueba = true;

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
        btnAbrirRegistro = root.Q<Button>("btn-abrir-registro");
        btnCerrarSesion = root.Q<Button>("btn-cerrar-sesion-terapeuta");
        formRegistro = root.Q<VisualElement>("form-registro");
        btnCerrarFormX = root.Q<Button>("btn-cerrar-x");
        mensajeLabel = root.Q<Label>("reg-mensaje");
        btnGuardarPrueba = root.Q<Button>("btn-guardar-prueba");
        panelReportes = root.Q<VisualElement>("panel-reportes");
        reportes = new ReportesPaciente(root);
        configTerapias = new ConfigTerapias(root);

        // Campos del formulario de registro
        regCedula = root.Q<TextField>("reg-cedula");
        regNombre = root.Q<TextField>("reg-nombre");
        regEmail = root.Q<TextField>("reg-email");
        regEdad = root.Q<IntegerField>("reg-edad");
        regContraseña = root.Q<TextField>("reg-contrasena");
        btnGuardarRegistro = root.Q<Button>("btn-guardar-registro");

        // Búsqueda de pacientes
        inputBusqueda = root.Q<TextField>("input-busqueda");
        btnBuscar = root.Q<Button>("btn-buscar");
        resultadoPaciente = root.Q<VisualElement>("resultado-paciente");
        lblResultadoNombre = root.Q<Label>("lbl-resultado-nombre");
        lblResultadoEdad = root.Q<Label>("lbl-resultado-edad");
        lblResultadoSesion = root.Q<Label>("lbl-resultado-sesion");

        if (resultadoPaciente != null) resultadoPaciente.style.display = DisplayStyle.None;
        if (btnBuscar != null) btnBuscar.clicked += BuscarPacientePorCedula;

        // Configurar campo de contraseña
        if (regContraseña != null) regContraseña.isPasswordField = true;

        // Conectar eventos
        if (btnAbrirRegistro != null) btnAbrirRegistro.clicked += AbrirModalRegistro;
        if (btnCerrarFormX != null) btnCerrarFormX.clicked += CerrarModalRegistro;
        if (btnGuardarRegistro != null) btnGuardarRegistro.clicked += RegistrarPaciente;
        if (btnCerrarSesion != null) btnCerrarSesion.clicked += CerrarSesion;
        if (btnGuardarPrueba != null) btnGuardarPrueba.clicked += GuardarSesionPrueba;

        // Conectar botón Ver Reportes
        Button btnVerReportes = root.Q<Button>("btn-ver-reportes");
        if (btnVerReportes != null)
        {
            btnVerReportes.clicked += () => {
                if (usarJsonPrueba)
                {
                    if (panelReportes != null) panelReportes.style.display = DisplayStyle.Flex;
                    CargarSesionesDePrueba();
                }
                else if (pacienteSeleccionado != null)
                {
                    if (panelReportes != null) panelReportes.style.display = DisplayStyle.Flex;
                    CargarSesionesPaciente(pacienteSeleccionado.id);
                }
                else
                {
                    MostrarMensajeRegistro("Selecciona un paciente primero", false);
                }
            };
        }

        // Botón CONFIGURAR TERAPIAS
        Button btnConfigurar = root.Q<Button>("btn-configurar");
        if (btnConfigurar != null)
        {
            btnConfigurar.clicked += () => {
                if (pacienteSeleccionado != null)
                    configTerapias.Abrir(pacienteSeleccionado);
                else
                    MostrarMensajeRegistro("Selecciona un paciente primero", false);
            };
        }

        Button btnCerrarReportes = root.Q<Button>("btn-cerrar-reportes");
        if (btnCerrarReportes != null)
            btnCerrarReportes.clicked += () => {
                if (panelReportes != null) panelReportes.style.display = DisplayStyle.None;
            };

        // Aseguramos que empiece oculto
        AlternarModal(formRegistro, false);
    }

    private void OnDisable()
    {
        if (btnAbrirRegistro != null) btnAbrirRegistro.clicked -= AbrirModalRegistro;
        if (btnCerrarFormX != null) btnCerrarFormX.clicked -= CerrarModalRegistro;
        if (btnGuardarRegistro != null) btnGuardarRegistro.clicked -= RegistrarPaciente;
        if (btnCerrarSesion != null) btnCerrarSesion.clicked -= CerrarSesion;
        if (btnGuardarPrueba != null) btnGuardarPrueba.clicked -= GuardarSesionPrueba;
        if (btnBuscar != null) btnBuscar.clicked -= BuscarPacientePorCedula;
    }

    private void AbrirModalRegistro() => AlternarModal(formRegistro, true);
    private void CerrarModalRegistro() => AlternarModal(formRegistro, false);

    private void AlternarModal(VisualElement modal, bool mostrar)
    {
        if (modal != null) modal.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void MostrarFormularioRegistro(bool mostrar)
    {
        if (formRegistro != null) formRegistro.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
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

    // ============================================
    // GET PACIENTES (AHORA SOLO GUARDA EN MEMORIA)
    // ============================================
    private IEnumerator GetPacientes(string idTerapeuta)
    {
        string url = $"{apiUrl}/mis-pacientes/{idTerapeuta}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string respuesta = request.downloadHandler.text;
                string jsonArray = "{\"pacientes\":" + respuesta + "}";
                PacienteListWrapper wrapper = JsonUtility.FromJson<PacienteListWrapper>(jsonArray);

                todosLosPacientes.Clear();
                if (wrapper.pacientes != null)
                {
                    todosLosPacientes.AddRange(wrapper.pacientes);
                    Debug.Log($"Se cargaron {todosLosPacientes.Count} pacientes en memoria.");
                }
            }
            else
            {
                Debug.LogError($"Error al cargar pacientes: {request.error}");
            }
        }
    }

    // ============================================
    // BUSCAR PACIENTE POR CÉDULA
    // ============================================
    private void BuscarPacientePorCedula()
    {
        string cedulaBuscada = inputBusqueda.value.Trim();
        Debug.Log($"[BUSCAR] Botón presionado. Cédula buscada: '{cedulaBuscada}'");

        if (string.IsNullOrEmpty(cedulaBuscada))
        {
            MostrarMensajeRegistro("Ingresa una cédula para buscar", false);
            return;
        }

        Debug.Log($"[BUSCAR] Pacientes en memoria: {todosLosPacientes.Count}");

        PacienteData pacienteEncontrado = todosLosPacientes.Find(p => p.cedula == cedulaBuscada);

        if (pacienteEncontrado != null)
        {
            Debug.Log($"[BUSCAR] ¡Paciente encontrado! {pacienteEncontrado.nombre}");
            resultadoPaciente.style.display = DisplayStyle.Flex;
            lblResultadoNombre.text = pacienteEncontrado.nombre;
            lblResultadoEdad.text = $"{pacienteEncontrado.edad} años";
            lblResultadoSesion.text = "Cargando última sesión...";

            pacienteSeleccionado = pacienteEncontrado;
            MostrarDetallePaciente(pacienteEncontrado);

            StartCoroutine(ObtenerFechaUltimaSesion(pacienteEncontrado.id));
        }
        else
        {
            Debug.Log($"[BUSCAR] No se encontró ningún paciente con la cédula: '{cedulaBuscada}'");
            resultadoPaciente.style.display = DisplayStyle.None;
            MostrarMensajeRegistro("No se encontró ningún paciente con esa cédula", false);
            MostrarDetallePaciente(null);
        }
    }

    // ============================================
    // OBTENER FECHA ÚLTIMA SESIÓN
    // ============================================
    private IEnumerator ObtenerFechaUltimaSesion(string idPaciente)
    {
        string url = $"http://localhost:5276/api/Sesiones/paciente/{idPaciente}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                if (json == "[]" || string.IsNullOrEmpty(json))
                {
                    lblResultadoSesion.text = "Sin sesiones registradas";
                }
                else
                {
                    string jsonArray = "{\"sesiones\":" + json + "}";
                    SesionesWrapper wrapper = JsonUtility.FromJson<SesionesWrapper>(jsonArray);

                    if (wrapper.sesiones != null && wrapper.sesiones.Length > 0)
                    {
                        SesionData sesionMasReciente = wrapper.sesiones[0];
                        DateTime fechaMasReciente = DateTime.MinValue;

                        foreach (var sesion in wrapper.sesiones)
                        {
                            DateTime fechaSesion;
                            if (DateTime.TryParse(sesion.fecha, out fechaSesion))
                            {
                                if (fechaSesion > fechaMasReciente)
                                {
                                    fechaMasReciente = fechaSesion;
                                    sesionMasReciente = sesion;
                                }
                            }
                        }

                        if (fechaMasReciente != DateTime.MinValue)
                        {
                            lblResultadoSesion.text = $"Última sesión: {fechaMasReciente.ToString("dd/MM/yyyy HH:mm")}";
                        }
                        else
                        {
                            lblResultadoSesion.text = "Sesiones sin fecha válida";
                        }
                    }
                    else
                    {
                        lblResultadoSesion.text = "Sin sesiones registradas";
                    }
                }
            }
            else
            {
                lblResultadoSesion.text = "Error al cargar sesión";
                Debug.LogError($"Error al obtener sesiones: {request.error}");
            }
        }
    }
    // ============================================
    // MOSTRAR DETALLE DEL PACIENTE (COLUMNA DERECHA)
    // ============================================
    private void MostrarDetallePaciente(PacienteData paciente)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Asegúrate de que estos nombres coincidan con los de tu UI Builder
        var detalleVacio = root.Q<VisualElement>("detalle-vacio");
        var detallePaciente = root.Q<VisualElement>("detalle-paciente");
        var lblNombre = root.Q<Label>("lbl-paciente-nombre");

        bool hayPaciente = paciente != null;

        // Si hay paciente, ocultamos el mensaje de "Selecciona un paciente" y mostramos el detalle
        if (detalleVacio != null)
            detalleVacio.style.display = hayPaciente ? DisplayStyle.None : DisplayStyle.Flex;

        if (detallePaciente != null)
            detallePaciente.style.display = hayPaciente ? DisplayStyle.Flex : DisplayStyle.None;

        // Actualizamos el nombre del paciente en la columna derecha
        if (hayPaciente && lblNombre != null)
            lblNombre.text = $"{paciente.nombre} · {paciente.cedula} · {paciente.edad} años";
    }

    // ============================================
    // REGISTRO DE PACIENTES
    // ============================================
    private void RegistrarPaciente()
    {
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
                CargarListaPacientes();
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
        var loginContainer = root.Q<VisualElement>("login");
        var contenidoTerapeuta = root.Q<VisualElement>("contenido-terapeuta");

        if (contenidoTerapeuta != null) contenidoTerapeuta.style.display = DisplayStyle.None;
        if (loginContainer != null) loginContainer.style.display = DisplayStyle.Flex;

        LoginManager.UsuarioActual = null;

        var cedulaInput = root.Q<TextField>("login-usuario");
        var passInput = root.Q<TextField>("login-password");
        if (cedulaInput != null) cedulaInput.value = "";
        if (passInput != null) passInput.value = "";

        var mensajeLogin = root.Q<Label>("mensaje-login");
        if (mensajeLogin != null) mensajeLogin.text = "";
    }

    // ============================================
    // GUARDAR SESIÓN DE PRUEBA
    // ============================================
    private void GuardarSesionPrueba()
    {
        if (LoginManager.UsuarioActual == null)
        {
            MostrarMensajeRegistro("Primero inicia sesión como terapeuta", false);
            return;
        }

        string idPacienteFijo = "6a120a7e3323054e9a659271"; // Reemplaza con un ID real de tu MongoDB

        SesionRequest sesionPrueba = new SesionRequest
        {
            IdPaciente = idPacienteFijo,
            IdTerapeuta = LoginManager.UsuarioActual.id,
            IdTerapia = 1,
            NombreTerapia = "Alcance (Prueba)",
            Puntaje = UnityEngine.Random.Range(40, 100),
            Precision = (float)UnityEngine.Random.Range(50f, 98f),
            TiempoSegundos = UnityEngine.Random.Range(30, 90),
            Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string jsonData = JsonUtility.ToJson(sesionPrueba);
        StartCoroutine(EnviarSesionPrueba(jsonData));
    }

    private IEnumerator EnviarSesionPrueba(string jsonData)
    {
        string url = "http://localhost:5276/api/Sesiones";
        MostrarMensajeRegistro("Guardando sesión de prueba...", true);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MostrarMensajeRegistro("Sesión de prueba guardada correctamente", true);
            }
            else
            {
                MostrarMensajeRegistro($"Error: {request.error}", false);
                Debug.LogError($"Error al guardar sesión: {request.error}");
            }
        }
    }

    // ============================================
    // REPORTES
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
        DibujarGrafica(wrapper.sesiones);
    }

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
} // <--- AQUÍ CIERRA LA CLASE TERAPEUTAPANEL (Solo una vez)

// ============================================
// CLASES DE DATOS (FUERA DE LA CLASE PRINCIPAL)
// ============================================
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

[System.Serializable]
public class PacienteData
{
    public string id;
    public string cedula;
    public string nombre;
    public string email;
    public int edad;
    public string rol;
    public string id_terapeuta;
}

[System.Serializable]
public class PacienteListWrapper
{
    public PacienteData[] pacientes;
}

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