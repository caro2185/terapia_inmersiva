using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DashboardEvolucion : MonoBehaviour
{
    // ============================================
    // DATOS
    // ============================================
    private DashboardData dataActual;
    private TerapiaData terapiaActual;
    private List<DiaAgrupado> diasAgrupados;

    // ============================================
    // REFERENCIAS UI
    // ============================================
    private VisualElement root;
    private VisualElement selectorTerapias;
    private VisualElement tarjetasResumen;
    private VisualElement puntosContainer;
    private VisualElement etiquetasX;
    private ScrollView cuerpoTabla;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        selectorTerapias = root.Q<VisualElement>("selector-terapias");
        tarjetasResumen = root.Q<VisualElement>("tarjetas-resumen");
        puntosContainer = root.Q<VisualElement>("puntos-container");
        etiquetasX = root.Q<VisualElement>("etiquetas-x");
        cuerpoTabla = root.Q<ScrollView>("cuerpo-tabla");

        // 1. Cargar datos mock (simula lo que vendría de la API)
        dataActual = GenerarDatosMock();

        // 2. Crear botones de selección de terapia
        CrearBotonesTerapias();

        // 3. Seleccionar la primera terapia por defecto
        SeleccionarTerapia(0);
    }

    // ============================================
    // GENERAR DATOS MOCK
    // ============================================
    private DashboardData GenerarDatosMock()
    {
        var data = new DashboardData
        {
            paciente = "Paciente de Prueba",
            diagnostico = "Rehabilitación motriz y cognitiva",
            terapias = new List<TerapiaData>()
        };

        // Terapia 1: Alcance (Motriz)
        data.terapias.Add(CrearTerapia(1, "Alcance", "Motriz", new (string, int, float, int, int, int, string)[]
        {
            ("2026-09-18 08:00:00", 45, 65.0f, 90, 12, 6, "Baja"),
            ("2026-09-18 08:15:00", 52, 72.0f, 85, 14, 5, "Baja"),
            ("2026-09-18 08:30:00", 48, 68.0f, 88, 13, 6, "Baja"),
            ("2026-09-19 08:00:00", 55, 75.0f, 80, 15, 5, "Baja"),
            ("2026-09-19 08:20:00", 58, 78.0f, 78, 16, 4, "Baja"),
            ("2026-09-21 08:00:00", 62, 80.0f, 75, 18, 4, "Media"),
            ("2026-09-21 08:20:00", 65, 82.0f, 72, 19, 3, "Media"),
            ("2026-09-21 08:40:00", 60, 79.0f, 76, 17, 4, "Media"),
            ("2026-09-23 08:00:00", 68, 85.0f, 70, 20, 3, "Media"),
            ("2026-09-23 08:20:00", 72, 87.0f, 68, 21, 2, "Media"),
            ("2026-09-24 08:00:00", 75, 89.0f, 65, 22, 2, "Media"),
            ("2026-09-24 08:20:00", 78, 91.0f, 62, 23, 2, "Alta"),
            ("2026-09-26 08:00:00", 82, 92.5f, 58, 24, 1, "Alta"),
            ("2026-09-28 08:00:00", 85, 94.0f, 55, 25, 1, "Alta"),
            ("2026-09-30 08:00:00", 88, 95.5f, 52, 26, 1, "Alta"),
        }));

        // Terapia 2: Agarre y soltar (Motriz)
        data.terapias.Add(CrearTerapia(2, "Agarre y soltar", "Motriz", new (string, int, float, int, int, int, string)[]
        {
            ("2026-09-18 09:00:00", 50, 68.0f, 95, 14, 6, "Baja"),
            ("2026-09-19 09:00:00", 58, 74.0f, 88, 16, 5, "Baja"),
            ("2026-09-21 09:00:00", 65, 80.0f, 80, 18, 4, "Media"),
            ("2026-09-23 09:00:00", 72, 85.0f, 72, 21, 3, "Media"),
            ("2026-09-25 09:00:00", 78, 89.0f, 65, 23, 2, "Alta"),
            ("2026-09-28 09:00:00", 84, 92.0f, 58, 25, 1, "Alta"),
        }));

        // Terapia 3: Memoria de secuencias (Cognitiva)
        data.terapias.Add(CrearTerapia(3, "Memoria de secuencias", "Cognitiva", new (string, int, float, int, int, int, string)[]
        {
            ("2026-09-18 10:00:00", 40, 60.0f, 100, 10, 8, "Baja"),
            ("2026-09-20 10:00:00", 48, 68.0f, 92, 13, 6, "Baja"),
            ("2026-09-22 10:00:00", 55, 74.0f, 85, 15, 5, "Media"),
            ("2026-09-24 10:00:00", 62, 80.0f, 78, 18, 4, "Media"),
            ("2026-09-26 10:00:00", 70, 85.0f, 70, 21, 3, "Alta"),
            ("2026-09-30 10:00:00", 78, 90.0f, 62, 24, 2, "Alta"),
        }));

        return data;
    }

    private TerapiaData CrearTerapia(int id, string nombre, string tipo, (string fecha, int puntaje, float precision, int tiempo, int aciertos, int errores, string dificultad)[] sesiones)
    {
        var t = new TerapiaData
        {
            id = id,
            nombre = nombre,
            tipo = tipo,
            sesiones = new List<SesionRaw>()
        };

        for (int i = 0; i < sesiones.Length; i++)
        {
            var s = sesiones[i];
            t.sesiones.Add(new SesionRaw
            {
                id = $"S{i + 1}",
                fecha = s.fecha,
                puntaje = s.puntaje,
                precision = s.precision,
                tiempoSegundos = s.tiempo,
                aciertos = s.aciertos,
                errores = s.errores,
                dificultad = s.dificultad,
                completada = true
            });
        }

        return t;
    }

    // ============================================
    // CREAR BOTONES DE TERAPIAS
    // ============================================
    private void CrearBotonesTerapias()
    {
        selectorTerapias.Clear();

        for (int i = 0; i < dataActual.terapias.Count; i++)
        {
            int index = i; // Capturar para el closure
            var terapia = dataActual.terapias[i];

            Button btn = new Button();
            btn.text = terapia.nombre;
            btn.clicked += () => SeleccionarTerapia(index);

            selectorTerapias.Add(btn);
        }
    }

    // ============================================
    // SELECCIONAR TERAPIA
    // ============================================
    private void SeleccionarTerapia(int index)
    {
        terapiaActual = dataActual.terapias[index];

        // Actualizar clase activa en botones
        for (int i = 0; i < selectorTerapias.childCount; i++)
        {
            var btn = selectorTerapias[i];
            if (i == index)
                btn.AddToClassList("activo");
            else
                btn.RemoveFromClassList("activo");
        }

        // Agrupar sesiones por día
        diasAgrupados = AgruparPorDia(terapiaActual.sesiones);

        // Actualizar toda la UI
        ActualizarTarjetasResumen();
        ActualizarGrafica();
        ActualizarTabla(diasAgrupados.LastOrDefault());
    }

    // ============================================
    // AGRUPAR POR DÍA
    // ============================================
    private List<DiaAgrupado> AgruparPorDia(List<SesionRaw> sesiones)
    {
        var grupos = new Dictionary<string, List<SesionRaw>>();

        foreach (var s in sesiones)
        {
            string fecha = s.fecha.Split(' ')[0];
            if (!grupos.ContainsKey(fecha))
                grupos[fecha] = new List<SesionRaw>();
            grupos[fecha].Add(s);
        }

        var resultado = new List<DiaAgrupado>();

        foreach (var kvp in grupos)
        {
            var sesionesDia = kvp.Value;

            var dia = new DiaAgrupado
            {
                fecha = DateTime.Parse(kvp.Key),
                numSesiones = sesionesDia.Count,
                puntajePromedio = (float)sesionesDia.Average(s => s.puntaje),
                precisionPromedio = (float)sesionesDia.Average(s => s.precision),
                tiempoPromedio = (float)sesionesDia.Average(s => s.tiempoSegundos),
                puntajeMin = sesionesDia.Min(s => s.puntaje),
                puntajeMax = sesionesDia.Max(s => s.puntaje),
                sesionesDelDia = sesionesDia
            };

            // Eficacia = 50% precisión + 30% puntaje + 20% factor tiempo
            float factorTiempo = Mathf.Clamp(100f - dia.tiempoPromedio, 0, 100);
            dia.eficaciaPromedio = (dia.precisionPromedio * 0.5f) + (dia.puntajePromedio * 0.3f) + (factorTiempo * 0.2f);

            resultado.Add(dia);
        }

        return resultado.OrderBy(d => d.fecha).ToList();
    }

    // ============================================
    // ACTUALIZAR TARJETAS DE RESUMEN
    // ============================================
    private void ActualizarTarjetasResumen()
    {
        int totalSesiones = terapiaActual.sesiones.Count;
        int diasConTerapia = diasAgrupados.Count;
        float eficaciaPromedio = 0f;

        foreach (var d in diasAgrupados)
            eficaciaPromedio += d.eficaciaPromedio;

        if (diasAgrupados.Count > 0)
            eficaciaPromedio /= diasAgrupados.Count;

        // Buscar los labels por nombre dentro de cada tarjeta
        var valorSesiones = tarjetasResumen.Q<Label>("valor-sesiones");
        var valorDias = tarjetasResumen.Q<Label>("valor-dias");
        var valorEficacia = tarjetasResumen.Q<Label>("valor-eficacia");

        if (valorSesiones != null) valorSesiones.text = totalSesiones.ToString();
        if (valorDias != null) valorDias.text = diasConTerapia.ToString();
        if (valorEficacia != null) valorEficacia.text = $"{eficaciaPromedio:F0}%";
    }

    // ============================================
    // ACTUALIZAR GRÁFICA
    // ============================================
    private void ActualizarGrafica()
    {
        puntosContainer.Clear();
        etiquetasX.Clear();

        if (diasAgrupados == null || diasAgrupados.Count == 0) return;

        foreach (var dia in diasAgrupados)
        {
            // Punto
            VisualElement punto = new VisualElement();
            punto.AddToClassList("punto-grafico");

            float porcentaje = Mathf.Clamp(dia.eficaciaPromedio, 0, 100);
            punto.style.marginBottom = Length.Percent(porcentaje);

            if (porcentaje >= 80) punto.AddToClassList("punto-verde");
            else if (porcentaje >= 60) punto.AddToClassList("punto-amarillo");
            else punto.AddToClassList("punto-rojo");

            puntosContainer.Add(punto);

            // Etiqueta
            Label etiqueta = new Label(dia.fecha.ToString("dd/MM"));
            etiquetasX.Add(etiqueta);
        }
    }

    // ============================================
    // ACTUALIZAR TABLA
    // ============================================
    private void ActualizarTabla(DiaAgrupado dia)
    {
        if (dia == null) return;

        cuerpoTabla.Clear();

        // Actualizar cabecera (por nombre)
        var lblTituloDia = root.Q<Label>("titulo-dia-tabla");
        if (lblTituloDia != null)
            lblTituloDia.text = $"{dia.fecha:dddd dd/MM/yyyy} · {terapiaActual.nombre}";

        // Encabezados
        string[] headers = { "SESIÓN", "HORA", "PUNTAJE", "PRECISIÓN", "ACIERTOS", "ERRORES", "TIEMPO", "DIFICULTAD" };
        var encabezados = root.Q<VisualElement>("encabezados-tabla");
        encabezados.Clear();
        foreach (var h in headers)
        {
            encabezados.Add(new Label(h));
        }

        // Filas
        foreach (var s in dia.sesionesDelDia)
        {
            VisualElement fila = new VisualElement();
            fila.AddToClassList("fila-tabla");

            fila.Add(new Label(s.id));
            fila.Add(new Label(s.fecha.Split(' ')[1].Substring(0, 5)));
            fila.Add(new Label($"{s.puntaje}%"));
            fila.Add(new Label($"{s.precision:F1}%"));
            fila.Add(new Label(s.aciertos.ToString()));
            fila.Add(new Label(s.errores.ToString()));
            fila.Add(new Label($"{s.tiempoSegundos} s"));
            fila.Add(new Label(s.dificultad));

            cuerpoTabla.Add(fila);
        }

        // Fila de promedio
        VisualElement filaPromedio = new VisualElement();
        filaPromedio.AddToClassList("fila-tabla");
        filaPromedio.AddToClassList("fila-promedio");

        filaPromedio.Add(new Label("Promedio"));
        filaPromedio.Add(new Label(""));
        filaPromedio.Add(new Label($"{dia.puntajePromedio:F0}%"));
        filaPromedio.Add(new Label($"{dia.precisionPromedio:F1}%"));
        filaPromedio.Add(new Label(""));
        filaPromedio.Add(new Label(""));
        filaPromedio.Add(new Label($"{dia.tiempoPromedio:F0} s"));
        filaPromedio.Add(new Label(""));

        cuerpoTabla.Add(filaPromedio);
    }
}
