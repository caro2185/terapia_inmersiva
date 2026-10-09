using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


    /// <summary>
    /// Arma y maneja todo el informe de reportes dentro de #reportes-contenido:
    /// pestañas por terapia, rango de fechas, resumen, gráfica por día y tabla de sesiones del día.
    /// No es un MonoBehaviour: TerapeutaPanel la crea y le pasa las sesiones.
    /// </summary>
    public class ReportesPaciente
    {
        class Terapia
        {
            public int id;
            public string nombre;
        }

        class Dia
        {
            public DateTime fecha;
            public List<SesionData> sesiones;
            public float promedio;
            public float minimo;
            public float maximo;
            public bool HayDatos { get { return sesiones != null && sesiones.Count > 0; } }
        }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly string[] NombresDia = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
        const string LetrasDia = "DLMXJVS";

        // Columnas de la tabla: título y ancho en píxeles
        static readonly string[] ColTitulos = { "Sesión", "Hora", "Puntaje", "Precisión", "Aciertos", "Errores", "Tiempo", "Dificultad", "Estado", "Notas" };
        static readonly float[] ColAnchos = { 56f, 54f, 80f, 84f, 76f, 68f, 66f, 86f, 92f, 220f };

        readonly ScrollView contenido;
        VisualElement tabsFila;
        VisualElement detalle;
        TextField tfDesde;
        TextField tfHasta;
        Button btn7;
        Button btnTodo;
        Label lblError;
        Label lblResumen;
        GraficaDiaria grafica;
        readonly List<Button> botonesTab = new List<Button>();

        SesionData[] todas = new SesionData[0];
        readonly List<Terapia> terapias = new List<Terapia>();
        readonly List<Dia> dias = new List<Dia>();
        int terapiaId;
        DateTime minFecha, maxFecha, desde, hasta;
        DateTime? diaSel;

        public ReportesPaciente(VisualElement root)
        {
            contenido = root.Q<ScrollView>("reportes-contenido");
            if (contenido == null)
            {
                Debug.LogError("ReportesPaciente: no existe un ScrollView llamado 'reportes-contenido' en el UXML.");
                return;
            }
            ConstruirInterfaz();
        }

        // =========================================================
        //  Construcción de la interfaz (una sola vez)
        // =========================================================
        void ConstruirInterfaz()
        {
            contenido.Clear();

            // Pestañas de terapia (se llenan en Mostrar)
            tabsFila = new VisualElement();
            tabsFila.AddToClassList("rp-tabs");
            contenido.Add(tabsFila);

            // Filtros de fecha
            VisualElement filtros = new VisualElement();
            filtros.AddToClassList("rp-filtros");

            tfDesde = CrearCampoFecha(filtros, "Desde");
            tfHasta = CrearCampoFecha(filtros, "Hasta");
            tfDesde.RegisterValueChangedCallback(evt => AplicarRango());
            tfHasta.RegisterValueChangedCallback(evt => AplicarRango());

            btn7 = new Button(() => { hasta = maxFecha; desde = maxFecha.AddDays(-6); if (desde < minFecha) desde = minFecha; LimpiarError(); Refrescar(); });
            btn7.text = "Últimos 7 días";
            btn7.AddToClassList("rp-btn");
            filtros.Add(btn7);

            btnTodo = new Button(() => { desde = minFecha; hasta = maxFecha; LimpiarError(); Refrescar(); });
            btnTodo.text = "Todo";
            btnTodo.AddToClassList("rp-btn");
            filtros.Add(btnTodo);
            contenido.Add(filtros);

            lblError = new Label();
            lblError.AddToClassList("rp-error");
            lblError.style.display = DisplayStyle.None;
            contenido.Add(lblError);

            // Resumen
            lblResumen = new Label();
            lblResumen.AddToClassList("rp-resumen");
            contenido.Add(lblResumen);

            // Gráfica
            VisualElement cajaGrafica = new VisualElement();
            cajaGrafica.AddToClassList("rp-grafica");
            grafica = new GraficaDiaria();
            grafica.PuntoSeleccionado += OnPuntoSeleccionado;
            cajaGrafica.Add(grafica);
            contenido.Add(cajaGrafica);

            // Leyenda
            VisualElement leyenda = new VisualElement();
            leyenda.AddToClassList("rp-leyenda");
            leyenda.Add(ItemLeyenda(GraficaDiaria.ColorPorValor(90f), "80% o más"));
            leyenda.Add(ItemLeyenda(GraficaDiaria.ColorPorValor(70f), "60 a 79%"));
            leyenda.Add(ItemLeyenda(GraficaDiaria.ColorPorValor(40f), "Menos de 60%"));
            contenido.Add(leyenda);

            // Detalle del día (título + tabla)
            detalle = new VisualElement();
            detalle.AddToClassList("rp-detalle");
            contenido.Add(detalle);
        }

        TextField CrearCampoFecha(VisualElement padre, string etiqueta)
        {
            VisualElement col = new VisualElement();
            col.AddToClassList("rp-campo");

            Label l = new Label(etiqueta);
            l.AddToClassList("rp-etq");
            col.Add(l);

            TextField tf = new TextField();
            tf.isDelayed = true;   // solo aplica al presionar Enter o salir del campo
            tf.AddToClassList("rp-fecha");
            col.Add(tf);

            padre.Add(col);
            return tf;
        }

        VisualElement ItemLeyenda(Color color, string texto)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("rp-leyenda-item");
            VisualElement punto = new VisualElement();
            punto.AddToClassList("rp-punto");
            punto.style.backgroundColor = color;
            item.Add(punto);
            Label l = new Label(texto);
            l.AddToClassList("rp-leyenda-texto");
            item.Add(l);
            return item;
        }

        // =========================================================
        //  API pública
        // =========================================================
        public void Mostrar(SesionData[] sesiones)
        {
            if (contenido == null) return;

            todas = sesiones ?? new SesionData[0];

            // Terapias presentes en los datos
            terapias.Clear();
            foreach (IGrouping<int, SesionData> g in todas.GroupBy(s => s.idTerapia).OrderBy(k => k.Key))
            {
                string nombre = g.First().nombreTerapia;
                if (string.IsNullOrEmpty(nombre)) nombre = "Terapia " + g.Key;
                terapias.Add(new Terapia { id = g.Key, nombre = nombre });
            }

            // Rango completo de fechas
            List<DateTime> fechas = new List<DateTime>();
            foreach (SesionData s in todas)
            {
                DateTime? f = FechaDe(s);
                if (f.HasValue) fechas.Add(f.Value.Date);
            }

            if (terapias.Count == 0 || fechas.Count == 0)
            {
                tabsFila.Clear();
                botonesTab.Clear();
                dias.Clear();
                grafica.Configurar(new GraficaDiaria.Punto[0], -1);
                lblResumen.text = "No hay sesiones para mostrar.";
                detalle.Clear();
                return;
            }

            minFecha = fechas.Min();
            maxFecha = fechas.Max();
            desde = minFecha;
            hasta = maxFecha;
            terapiaId = terapias[0].id;
            diaSel = null;

            ConstruirTabs();
            LimpiarError();
            Refrescar();
        }

        // =========================================================
        //  Pestañas y filtros
        // =========================================================
        void ConstruirTabs()
        {
            tabsFila.Clear();
            botonesTab.Clear();
            foreach (Terapia t in terapias)
            {
                int id = t.id;
                Button b = new Button(() => { terapiaId = id; Refrescar(); });
                b.text = t.nombre;
                b.userData = id;
                b.AddToClassList("rp-tab");
                tabsFila.Add(b);
                botonesTab.Add(b);
            }
        }

        void AplicarRango()
        {
            DateTime d, h;
            if (!DateTime.TryParseExact(tfDesde.value.Trim(), "yyyy-MM-dd", Inv, DateTimeStyles.None, out d) ||
                !DateTime.TryParseExact(tfHasta.value.Trim(), "yyyy-MM-dd", Inv, DateTimeStyles.None, out h))
            {
                MostrarError("Usa el formato aaaa-mm-dd. Ejemplo: 2026-09-30");
                return;
            }
            if (d > h)
            {
                MostrarError("La fecha inicial debe ser anterior o igual a la final.");
                return;
            }
            LimpiarError();
            desde = d;
            hasta = h;
            Refrescar();
        }

        void MostrarError(string msg)
        {
            lblError.text = msg;
            lblError.style.display = DisplayStyle.Flex;
        }

        void LimpiarError()
        {
            lblError.text = "";
            lblError.style.display = DisplayStyle.None;
        }

        // =========================================================
        //  Agrupar por día y refrescar todo
        // =========================================================
        void Refrescar()
        {
            // Estado visual de pestañas y filtros
            foreach (Button b in botonesTab)
                b.EnableInClassList("rp-tab--activa", (int)b.userData == terapiaId);

            tfDesde.SetValueWithoutNotify(desde.ToString("yyyy-MM-dd", Inv));
            tfHasta.SetValueWithoutNotify(hasta.ToString("yyyy-MM-dd", Inv));
            btn7.EnableInClassList("rp-btn--activo", hasta == maxFecha && desde == maxFecha.AddDays(-6));
            btnTodo.EnableInClassList("rp-btn--activo", desde == minFecha && hasta == maxFecha);

            // Agrupar las sesiones de la terapia por día
            Dictionary<DateTime, List<SesionData>> porDia = new Dictionary<DateTime, List<SesionData>>();
            foreach (SesionData s in todas)
            {
                if (s.idTerapia != terapiaId) continue;
                DateTime? f = FechaDe(s);
                if (!f.HasValue) continue;
                DateTime d = f.Value.Date;
                if (d < desde || d > hasta) continue;
                if (!porDia.ContainsKey(d)) porDia[d] = new List<SesionData>();
                porDia[d].Add(s);
            }

            // Un registro por cada día del rango (con o sin sesiones)
            dias.Clear();
            for (DateTime d = desde; d <= hasta; d = d.AddDays(1))
            {
                Dia dia = new Dia { fecha = d };
                List<SesionData> lista;
                if (porDia.TryGetValue(d, out lista))
                {
                    lista = lista.OrderBy(x => FechaDe(x) ?? DateTime.MinValue).ToList();
                    dia.sesiones = lista;
                    dia.promedio = (float)lista.Average(x => x.puntaje);
                    dia.minimo = lista.Min(x => x.puntaje);
                    dia.maximo = lista.Max(x => x.puntaje);
                }
                dias.Add(dia);
            }

            // Día seleccionado: si ya no tiene datos, se usa el último día con sesiones
            if (!diaSel.HasValue || !dias.Any(x => x.HayDatos && x.fecha == diaSel.Value))
            {
                Dia ultimo = dias.LastOrDefault(x => x.HayDatos);
                diaSel = ultimo != null ? ultimo.fecha : (DateTime?)null;
            }

            // Puntos para la gráfica
            GraficaDiaria.Punto[] puntos = new GraficaDiaria.Punto[dias.Count];
            for (int i = 0; i < dias.Count; i++)
            {
                Dia x = dias[i];
                string etiqueta = x.fecha == DateTime.Today ? "Hoy"
                                : x.fecha == DateTime.Today.AddDays(-1) ? "Ayer"
                                : x.fecha.ToString("dd/MM", Inv);
                puntos[i] = new GraficaDiaria.Punto
                {
                    hayDatos = x.HayDatos,
                    promedio = x.promedio,
                    minimo = x.minimo,
                    maximo = x.maximo,
                    cantidad = x.HayDatos ? x.sesiones.Count : 0,
                    etiqueta = etiqueta,
                    diaSemana = LetrasDia[(int)x.fecha.DayOfWeek].ToString(),
                    titulo = TituloDia(x.fecha)
                };
            }
            int indiceSel = diaSel.HasValue ? dias.FindIndex(x => x.fecha == diaSel.Value) : -1;
            grafica.Configurar(puntos, indiceSel);

            ActualizarResumen();
            ActualizarDetalle();
        }

        void ActualizarResumen()
        {
            List<Dia> con = dias.Where(x => x.HayDatos).ToList();
            if (con.Count == 0)
            {
                lblResumen.text = "Sin sesiones de esta terapia en el rango elegido.";
                return;
            }

            List<SesionData> ses = con.SelectMany(x => x.sesiones).ToList();
            float prom = (float)ses.Average(s => s.puntaje);

            string cambio = "";
            if (con.Count > 1)
            {
                int dif = Mathf.RoundToInt(con[con.Count - 1].promedio - con[0].promedio);
                cambio = " · cambio " + (dif > 0 ? "+" : "") + dif + " pts";
            }

            lblResumen.text = ses.Count + " sesiones · " + con.Count + " días con terapia · eficacia promedio " +
                              prom.ToString("0", Inv) + "%" + cambio;
        }

        // =========================================================
        //  Detalle del día: título + tabla
        // =========================================================
        void OnPuntoSeleccionado(int indice)
        {
            if (indice < 0 || indice >= dias.Count || !dias[indice].HayDatos) return;
            diaSel = dias[indice].fecha;
            ActualizarDetalle();
        }

        void IrADia(List<Dia> con, int pos)
        {
            if (pos < 0 || pos >= con.Count) return;
            diaSel = con[pos].fecha;
            grafica.Seleccionar(dias.IndexOf(con[pos]));
            ActualizarDetalle();
        }

        void ActualizarDetalle()
        {
            detalle.Clear();

            Dia dia = diaSel.HasValue ? dias.FirstOrDefault(x => x.HayDatos && x.fecha == diaSel.Value) : null;
            if (dia == null)
            {
                Label vacio = new Label("Sin sesiones en el rango. Amplía las fechas para ver el detalle.");
                vacio.AddToClassList("rp-vacio");
                detalle.Add(vacio);
                return;
            }

            List<Dia> con = dias.Where(x => x.HayDatos).ToList();
            int pos = con.IndexOf(dia);
            Terapia terapia = terapias.First(t => t.id == terapiaId);

            // Cabecera: título, subtítulo y botones anterior/siguiente
            VisualElement cab = new VisualElement();
            cab.AddToClassList("rp-detalle-cab");

            VisualElement textos = new VisualElement();
            textos.AddToClassList("rp-detalle-textos");
            Label titulo = new Label(TituloDia(dia.fecha));
            titulo.AddToClassList("rp-detalle-titulo");
            Label sub = new Label(terapia.nombre + " · " + dia.sesiones.Count + " sesiones · promedio " +
                                  dia.promedio.ToString("0", Inv) + "% · rango " + dia.minimo.ToString("0", Inv) +
                                  " a " + dia.maximo.ToString("0", Inv) + "%");
            sub.AddToClassList("rp-detalle-sub");
            textos.Add(titulo);
            textos.Add(sub);
            cab.Add(textos);

            VisualElement nav = new VisualElement();
            nav.AddToClassList("rp-nav");
            Button ant = new Button(() => IrADia(con, pos - 1));
            ant.text = "<";
            ant.AddToClassList("rp-nav-btn");
            ant.SetEnabled(pos > 0);
            Button sig = new Button(() => IrADia(con, pos + 1));
            sig.text = ">";
            sig.AddToClassList("rp-nav-btn");
            sig.SetEnabled(pos < con.Count - 1);
            nav.Add(ant);
            nav.Add(sig);
            cab.Add(nav);
            detalle.Add(cab);

            // Tabla (scroll horizontal por si el panel es angosto)
            ScrollView tabla = new ScrollView(ScrollViewMode.Horizontal);
            tabla.AddToClassList("rp-tabla");

            float total = ColAnchos.Sum();

            // Un ScrollView horizontal coloca a sus hijos uno al lado del otro.
            // Por eso las filas van dentro de este contenedor en columna, que es el único hijo del ScrollView.
            VisualElement cuerpo = new VisualElement();
            cuerpo.style.flexDirection = FlexDirection.Column;
            cuerpo.style.flexShrink = 0;
            cuerpo.style.width = total;

            VisualElement filaCab = NuevaFila("rp-fila rp-fila--cab", total);
            for (int c = 0; c < ColTitulos.Length; c++)
                filaCab.Add(Celda(ColTitulos[c], c));
            cuerpo.Add(filaCab);

            for (int i = 0; i < dia.sesiones.Count; i++)
            {
                SesionData s = dia.sesiones[i];
                DateTime? f = FechaDe(s);

                VisualElement fila = NuevaFila("rp-fila", total);
                fila.Add(Celda("S" + (i + 1), 0));
                fila.Add(Celda(f.HasValue ? f.Value.ToString("HH:mm", Inv) : "--:--", 1));
                fila.Add(CeldaPuntaje(s.puntaje, 2));
                fila.Add(Celda(s.precision.ToString("0.0", Inv) + "%", 3));
                fila.Add(Celda(s.aciertos.ToString(), 4));
                fila.Add(Celda(s.errores.ToString(), 5));
                fila.Add(Celda(s.tiempoSegundos + " s", 6));
                fila.Add(Celda(Capitalizar(s.dificultad), 7));

                Label estado = Celda(s.completada ? "Completada" : "Abandonó", 8);
                if (!s.completada) estado.AddToClassList("rp-celda--alerta");
                fila.Add(estado);

                Label notas = Celda(string.IsNullOrEmpty(s.notas) ? "-" : s.notas, 9);
                notas.AddToClassList("rp-celda--notas");
                fila.Add(notas);

                cuerpo.Add(fila);
            }

            // Fila de promedios del día
            VisualElement pie = NuevaFila("rp-fila rp-fila--pie", total);
            pie.Add(Celda("Prom.", 0));
            pie.Add(Celda("", 1));
            pie.Add(CeldaPuntaje(dia.promedio, 2));
            pie.Add(Celda(dia.sesiones.Average(x => x.precision).ToString("0.0", Inv) + "%", 3));
            pie.Add(Celda(dia.sesiones.Average(x => x.aciertos).ToString("0.0", Inv), 4));
            pie.Add(Celda(dia.sesiones.Average(x => x.errores).ToString("0.0", Inv), 5));
            pie.Add(Celda(dia.sesiones.Average(x => x.tiempoSegundos).ToString("0", Inv) + " s", 6));
            pie.Add(Celda("", 7));
            pie.Add(Celda("", 8));
            pie.Add(Celda("", 9));
            cuerpo.Add(pie);

            tabla.Add(cuerpo);
            detalle.Add(tabla);
        }

        VisualElement NuevaFila(string clases, float ancho)
        {
            VisualElement fila = new VisualElement();
            foreach (string c in clases.Split(' ')) fila.AddToClassList(c);
            fila.style.width = ancho;
            return fila;
        }

        Label Celda(string texto, int columna)
        {
            Label l = new Label(texto);
            l.AddToClassList("rp-celda");
            l.style.width = ColAnchos[columna];
            return l;
        }

        VisualElement CeldaPuntaje(float valor, int columna)
        {
            VisualElement cont = new VisualElement();
            cont.AddToClassList("rp-celda-puntaje");
            cont.style.width = ColAnchos[columna];

            VisualElement punto = new VisualElement();
            punto.AddToClassList("rp-punto");
            punto.style.backgroundColor = GraficaDiaria.ColorPorValor(valor);
            cont.Add(punto);

            Label l = new Label(valor.ToString("0", Inv) + "%");
            l.AddToClassList("rp-celda-texto");
            cont.Add(l);
            return cont;
        }

        // =========================================================
        //  Utilidades
        // =========================================================
        static DateTime? FechaDe(SesionData s)
        {
            DateTime f;
            if (DateTime.TryParse(s.fecha, Inv, DateTimeStyles.None, out f)) return f;
            return null;
        }

        static string TituloDia(DateTime d)
        {
            return NombresDia[(int)d.DayOfWeek] + " " + d.ToString("dd/MM/yyyy", Inv);
        }

        static string Capitalizar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }

