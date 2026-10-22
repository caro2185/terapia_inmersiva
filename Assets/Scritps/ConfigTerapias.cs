using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Controla la pantalla "Configurar terapias".
///
/// La ESTRUCTURA vive en Mimenu.uxml (panel-config-terapias) y el ASPECTO en styles.uss.
/// Esta clase solo hace dos cosas:
///   1. Busca los elementos por nombre y conecta los botones.
///   2. Llena lo que depende del catálogo: la lista de terapias y las variables.
///
/// No es un MonoBehaviour: TerapeutaPanel la crea con  new ConfigTerapias(root)
/// y la abre con  Abrir(paciente).
/// </summary>
public class ConfigTerapias
{
    // ------------------------------------------------------------
    // Nombres de los elementos del UXML (si cambias uno allá, cámbialo aquí)
    // ------------------------------------------------------------
    private const string PanelName = "panel-config-terapias";
    private const string TituloName = "lbl-config-titulo";
    private const string CerrarName = "btn-cerrar-config";
    private const string ListaName = "lista-terapias";
    private const string NombreTerapiaName = "lbl-terapia-nombre";
    private const string ObjetivoName = "lbl-terapia-objetivo";
    private const string VariablesName = "contenedor-variables";
    private const string EstadoName = "lbl-config-estado";
    private const string VerTerapiaName = "btn-ver-terapia";

    // Clases USS que se agregan por código (los elementos que crea esta clase)
    private const string ClaseTerapia = "cfg-ter";
    private const string ClaseTerapiaActiva = "cfg-ter--activa";
    private const string ClaseNivelActivo = "cfg-nivel--activo";

    private static readonly string[] Niveles = { "baja", "media", "alta" };

    // Lo configurado se recuerda mientras la aplicación esté abierta.
    // Más adelante esto se guardará en la API / MongoDB.
    private class ConfigGuardada
    {
        public string nivel;
        public Dictionary<string, float> valores;
    }
    private static readonly Dictionary<string, ConfigGuardada> guardadas =
        new Dictionary<string, ConfigGuardada>();

    // ------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------
    private readonly CatalogoTerapias catalogo;
    private readonly Dictionary<string, Button> botonesNivel = new Dictionary<string, Button>();

    private VisualElement panel;
    private Label lblTitulo;
    private ScrollView listaTerapias;
    private Label lblNombreTerapia;
    private Label lblObjetivo;
    private VisualElement contenedorVariables;
    private Label lblEstado;

    private PacienteData paciente;
    private int indiceActual = -1;
    private string nivelActual = "media";
    private Dictionary<string, float> valoresActuales = new Dictionary<string, float>();

    // ------------------------------------------------------------
    // CONSTRUCTOR: busca elementos y conecta eventos (una sola vez)
    // ------------------------------------------------------------
    public ConfigTerapias(VisualElement root)
    {
        catalogo = CatalogoTerapias.Cargar();

        panel = root.Q<VisualElement>(PanelName);
        if (panel == null)
        {
            Debug.LogError("ConfigTerapias: no se encontró '" + PanelName + "' en el UXML.");
            return;
        }

        lblTitulo = panel.Q<Label>(TituloName);
        listaTerapias = panel.Q<ScrollView>(ListaName);
        lblNombreTerapia = panel.Q<Label>(NombreTerapiaName);
        lblObjetivo = panel.Q<Label>(ObjetivoName);
        contenedorVariables = panel.Q<VisualElement>(VariablesName);
        lblEstado = panel.Q<Label>(EstadoName);

        Button btnCerrar = panel.Q<Button>(CerrarName);
        if (btnCerrar != null) btnCerrar.clicked += Cerrar;

        Button btnVer = panel.Q<Button>(VerTerapiaName);
        if (btnVer != null) btnVer.clicked += VerTerapia;

        foreach (string nivel in Niveles)
        {
            string nombreNivel = nivel; // copia local para el callback
            Button boton = panel.Q<Button>("btn-nivel-" + nivel);
            if (boton == null) continue;

            boton.clicked += () => CambiarNivel(nombreNivel);
            botonesNivel[nivel] = boton;
        }
    }

    // ------------------------------------------------------------
    // ABRIR / CERRAR
    // ------------------------------------------------------------
    public void Abrir(PacienteData pacienteElegido)
    {
        if (panel == null) return;

        paciente = pacienteElegido;

        if (lblTitulo != null)
            lblTitulo.text = "Configurar terapias · " + (paciente != null ? paciente.nombre : "");

        MostrarEstado("");

        if (catalogo.terapias.Length > 0)
        {
            Seleccionar(0);
        }
        else
        {
            indiceActual = -1;
            listaTerapias.Clear();
            contenedorVariables.Clear();
            lblNombreTerapia.text = "Sin terapias";
            lblObjetivo.text = "No hay terapias en Resources/terapias.json.";
        }

        panel.style.display = DisplayStyle.Flex;
        panel.BringToFront();
    }

    private void Cerrar()
    {
        panel.style.display = DisplayStyle.None;
    }

    // ------------------------------------------------------------
    // SELECCIONAR UNA TERAPIA
    // ------------------------------------------------------------
    private void Seleccionar(int indice)
    {
        indiceActual = indice;
        TerapiaDef terapia = catalogo.terapias[indice];

        ConfigGuardada guardada;
        if (guardadas.TryGetValue(ClaveGuardado(terapia), out guardada))
        {
            nivelActual = guardada.nivel;
            valoresActuales = new Dictionary<string, float>(guardada.valores);
        }
        else
        {
            nivelActual = "media";
            CargarValoresDelNivel(terapia);
        }

        MostrarEstado("");
        PintarLista();
        PintarDetalle();
    }

    // Carga en valoresActuales los valores predefinidos del nivel elegido
    private void CargarValoresDelNivel(TerapiaDef terapia)
    {
        valoresActuales = new Dictionary<string, float>();
        if (terapia.variables == null) return;

        foreach (VariableDef variable in terapia.variables)
        {
            valoresActuales[variable.clave] =
                variable.porNivel != null ? variable.porNivel.Obtener(nivelActual) : variable.min;
        }
    }

    private string ClaveGuardado(TerapiaDef terapia)
    {
        return (paciente != null ? paciente.id : "") + "|" + terapia.id;
    }

    private void RecordarConfiguracion()
    {
        if (indiceActual < 0) return;

        guardadas[ClaveGuardado(catalogo.terapias[indiceActual])] = new ConfigGuardada
        {
            nivel = nivelActual,
            valores = new Dictionary<string, float>(valoresActuales)
        };
    }

    // ------------------------------------------------------------
    // LISTA DE TERAPIAS (columna izquierda)
    // ------------------------------------------------------------
    private void PintarLista()
    {
        listaTerapias.Clear();

        for (int i = 0; i < catalogo.terapias.Length; i++)
        {
            int indice = i; // copia local para el callback
            TerapiaDef terapia = catalogo.terapias[i];

            VisualElement tarjeta = new VisualElement();
            tarjeta.AddToClassList(ClaseTerapia);
            if (i == indiceActual) tarjeta.AddToClassList(ClaseTerapiaActiva);

            Label numero = new Label("Terapia " + (i + 1));
            numero.AddToClassList("cfg-ter-n");
            Label nombre = new Label(terapia.nombre);
            nombre.AddToClassList("cfg-ter-nombre");

            tarjeta.Add(numero);
            tarjeta.Add(nombre);
            tarjeta.RegisterCallback<ClickEvent>(evt => Seleccionar(indice));

            listaTerapias.Add(tarjeta);
        }
    }

    // ------------------------------------------------------------
    // DETALLE DE LA TERAPIA (columna derecha)
    // ------------------------------------------------------------
    private void PintarDetalle()
    {
        TerapiaDef terapia = catalogo.terapias[indiceActual];

        lblNombreTerapia.text = terapia.nombre;
        lblObjetivo.text = terapia.objetivo;

        MarcarNivelActivo();
        PintarVariables(terapia);
    }

    private void MarcarNivelActivo()
    {
        foreach (KeyValuePair<string, Button> par in botonesNivel)
            par.Value.EnableInClassList(ClaseNivelActivo, par.Key == nivelActual);
    }

    private void PintarVariables(TerapiaDef terapia)
    {
        contenedorVariables.Clear();

        if (terapia.variables == null || terapia.variables.Length == 0)
        {
            Label vacio = new Label("Esta terapia no tiene variables configurables.");
            vacio.AddToClassList("cfg-var-etq");
            contenedorVariables.Add(vacio);
            return;
        }

        foreach (VariableDef variable in terapia.variables)
            contenedorVariables.Add(CrearFilaVariable(variable));
    }

    // Una fila: etiqueta + valor actual + slider
    private VisualElement CrearFilaVariable(VariableDef variable)
    {
        VisualElement fila = new VisualElement();
        fila.AddToClassList("cfg-var");

        VisualElement cabecera = new VisualElement();
        cabecera.AddToClassList("cfg-var-cab");

        Label etiqueta = new Label(variable.etiqueta);
        etiqueta.AddToClassList("cfg-var-etq");

        Label valorTexto = new Label(FormatearValor(valoresActuales[variable.clave], variable.unidad));
        valorTexto.AddToClassList("cfg-var-valor");

        cabecera.Add(etiqueta);
        cabecera.Add(valorTexto);
        fila.Add(cabecera);

        Slider slider = new Slider(variable.min, variable.max);
        slider.AddToClassList("cfg-slider");
        slider.SetValueWithoutNotify(valoresActuales[variable.clave]);
        slider.RegisterValueChangedCallback(evento =>
        {
            float valor = AjustarAlPaso(evento.newValue, variable);
            slider.SetValueWithoutNotify(valor);

            valoresActuales[variable.clave] = valor;
            valorTexto.text = FormatearValor(valor, variable.unidad);
            RecordarConfiguracion();
        });
        fila.Add(slider);

        return fila;
    }

    // ------------------------------------------------------------
    // NIVEL
    // ------------------------------------------------------------
    private void CambiarNivel(string nuevoNivel)
    {
        if (indiceActual < 0) return;

        nivelActual = nuevoNivel;
        CargarValoresDelNivel(catalogo.terapias[indiceActual]);
        RecordarConfiguracion();

        MarcarNivelActivo();
        PintarVariables(catalogo.terapias[indiceActual]);
    }

    // ------------------------------------------------------------
    // VER TERAPIA: entrega la configuración a la escena y la abre
    // ------------------------------------------------------------
    private void VerTerapia()
    {
        if (indiceActual < 0 || paciente == null) return;

        TerapiaDef terapia = catalogo.terapias[indiceActual];

        if (string.IsNullOrEmpty(terapia.escena) || !Application.CanStreamedLevelBeLoaded(terapia.escena))
        {
            MostrarEstado("La escena '" + terapia.escena + "' no está en Build Settings.");
            return;
        }

        RecordarConfiguracion();
        ConfiguracionActiva.Establecer(
            paciente.id,
            paciente.nombre,
            terapia,
            nivelActual,
            valoresActuales,
            SceneManager.GetActiveScene().name);

        SceneManager.LoadScene(terapia.escena);
    }

    // ------------------------------------------------------------
    // AYUDAS
    // ------------------------------------------------------------
    private void MostrarEstado(string mensaje)
    {
        if (lblEstado != null) lblEstado.text = mensaje;
    }

    // Ajusta el valor del slider al paso definido en el catálogo
    private static float AjustarAlPaso(float valor, VariableDef variable)
    {
        float paso = variable.paso > 0f ? variable.paso : 1f;
        float ajustado = Mathf.Round((valor - variable.min) / paso) * paso + variable.min;
        return Mathf.Clamp(ajustado, variable.min, variable.max);
    }

    private static string FormatearValor(float valor, string unidad)
    {
        string texto = valor.ToString("0.##");
        return string.IsNullOrEmpty(unidad) ? texto : texto + " " + unidad;
    }
}
