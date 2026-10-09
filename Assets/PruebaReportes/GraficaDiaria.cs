using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


    /// <summary>
    /// Gráfica de eficacia (%) por día, dibujada con vectores (Painter2D).
    /// Un punto por día = promedio de las sesiones de ese día.
    /// Barra fina = mínimo y máximo del día. Clic en un punto = selecciona el día.
    /// </summary>
    public class GraficaDiaria : VisualElement
    {
        public struct Punto
        {
            public bool hayDatos;
            public float promedio;
            public float minimo;
            public float maximo;
            public int cantidad;
            public string etiqueta;    // "30/09", "Hoy", "Ayer"
            public string diaSemana;   // "L", "M", "X"...
            public string titulo;      // "Miércoles 30/09/2026" (tooltip)
        }

        public event Action<int> PuntoSeleccionado;

        // Márgenes internos (espacio para números, títulos de ejes)
        const float MargenIzq = 40f;
        const float MargenDer = 12f;
        const float MargenSup = 26f;
        const float MargenInf = 54f;
        const float RadioPunto = 6f;

        static readonly Color ColorFondoPunto = new Color(0.12f, 0.12f, 0.16f, 1f);
        static readonly Color ColorRejilla = new Color(1f, 1f, 1f, 0.10f);
        static readonly Color ColorEje = new Color(1f, 1f, 1f, 0.55f);
        static readonly Color ColorTexto = new Color(1f, 1f, 1f, 0.75f);
        static readonly Color ColorLinea = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color ColorRango = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color BandaRoja = new Color(0.90f, 0.20f, 0.20f, 0.10f);
        static readonly Color BandaAmarilla = new Color(0.92f, 0.76f, 0.15f, 0.10f);
        static readonly Color BandaVerde = new Color(0.13f, 0.83f, 0.42f, 0.10f);
        static readonly Color ColorSeleccion = new Color(0.42f, 0.82f, 0.18f, 0.16f);

        public static Color ColorPorValor(float v)
        {
            if (v >= 80f) return new Color(0.13f, 0.83f, 0.42f);
            if (v >= 60f) return new Color(0.92f, 0.76f, 0.15f);
            return new Color(0.90f, 0.20f, 0.20f);
        }

        Punto[] puntos = new Punto[0];
        int seleccionado = -1;
        readonly List<Label> textos = new List<Label>();
        readonly Label tooltip;

        public GraficaDiaria()
        {
            style.flexGrow = 1;
            style.minHeight = 280;

            generateVisualContent += Dibujar;
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                ReconstruirTextos();
                MarkDirtyRepaint();
            });
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(evt => tooltip.style.display = DisplayStyle.None);
            RegisterCallback<ClickEvent>(OnClick);

            tooltip = new Label();
            tooltip.pickingMode = PickingMode.Ignore;
            tooltip.style.position = Position.Absolute;
            tooltip.style.display = DisplayStyle.None;
            tooltip.style.whiteSpace = WhiteSpace.NoWrap;
            tooltip.style.fontSize = 11;
            tooltip.style.color = Color.white;
            tooltip.style.backgroundColor = new Color(0f, 0f, 0f, 0.92f);
            tooltip.style.paddingLeft = 8;
            tooltip.style.paddingRight = 8;
            tooltip.style.paddingTop = 4;
            tooltip.style.paddingBottom = 4;
            tooltip.style.unityTextAlign = TextAnchor.MiddleLeft;
            tooltip.style.borderTopLeftRadius = 5;
            tooltip.style.borderTopRightRadius = 5;
            tooltip.style.borderBottomLeftRadius = 5;
            tooltip.style.borderBottomRightRadius = 5;
            Add(tooltip);
        }

        // =========================================================
        //  API pública
        // =========================================================
        public void Configurar(Punto[] datos, int indiceSeleccionado)
        {
            puntos = datos ?? new Punto[0];
            seleccionado = indiceSeleccionado;
            tooltip.style.display = DisplayStyle.None;
            ReconstruirTextos();
            MarkDirtyRepaint();
        }

        public void Seleccionar(int indice)
        {
            seleccionado = indice;
            MarkDirtyRepaint();
        }

        // =========================================================
        //  Geometría
        // =========================================================
        Rect AreaPlot()
        {
            Rect r = contentRect;
            return new Rect(
                MargenIzq,
                MargenSup,
                Mathf.Max(1f, r.width - MargenIzq - MargenDer),
                Mathf.Max(1f, r.height - MargenSup - MargenInf));
        }

        static float PosX(Rect a, int i, int n) { return a.xMin + a.width * (i + 0.5f) / n; }
        static float PosY(Rect a, float v) { return a.yMax - a.height * Mathf.Clamp(v, 0f, 100f) / 100f; }

        // =========================================================
        //  Dibujo
        // =========================================================
        void Dibujar(MeshGenerationContext ctx)
        {
            Painter2D p = ctx.painter2D;
            Rect a = AreaPlot();
            if (a.width < 10f || a.height < 10f) return;

            int n = puntos.Length;

            // 1) Bandas de color por rango
            Banda(p, a, 0f, 60f, BandaRoja);
            Banda(p, a, 60f, 80f, BandaAmarilla);
            Banda(p, a, 80f, 100f, BandaVerde);

            // 2) Columna del día seleccionado
            if (n > 0 && seleccionado >= 0 && seleccionado < n)
            {
                float w = a.width / n;
                float x0 = a.xMin + w * seleccionado;
                p.fillColor = ColorSeleccion;
                p.BeginPath();
                p.MoveTo(new Vector2(x0, a.yMin));
                p.LineTo(new Vector2(x0 + w, a.yMin));
                p.LineTo(new Vector2(x0 + w, a.yMax));
                p.LineTo(new Vector2(x0, a.yMax));
                p.ClosePath();
                p.Fill();
            }

            // 3) Rejilla horizontal y eje X
            p.lineWidth = 1f;
            for (int v = 0; v <= 100; v += 20)
            {
                p.strokeColor = (v == 0) ? ColorEje : ColorRejilla;
                p.BeginPath();
                p.MoveTo(new Vector2(a.xMin, PosY(a, v)));
                p.LineTo(new Vector2(a.xMax, PosY(a, v)));
                p.Stroke();
            }

            // 4) Eje Y
            p.strokeColor = ColorEje;
            p.BeginPath();
            p.MoveTo(new Vector2(a.xMin, a.yMin));
            p.LineTo(new Vector2(a.xMin, a.yMax));
            p.Stroke();

            if (n == 0) return;

            // 5) Línea que une los días con datos (se corta en días sin sesiones)
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            p.strokeColor = ColorLinea;
            bool abierta = false;
            for (int i = 0; i < n; i++)
            {
                if (!puntos[i].hayDatos)
                {
                    if (abierta) { p.Stroke(); abierta = false; }
                    continue;
                }
                Vector2 pt = new Vector2(PosX(a, i, n), PosY(a, puntos[i].promedio));
                if (!abierta) { p.BeginPath(); p.MoveTo(pt); abierta = true; }
                else p.LineTo(pt);
            }
            if (abierta) p.Stroke();

            // 6) Barra mínimo a máximo de cada día
            p.lineWidth = 2f;
            p.strokeColor = ColorRango;
            for (int i = 0; i < n; i++)
            {
                if (!puntos[i].hayDatos || puntos[i].maximo - puntos[i].minimo < 0.5f) continue;
                float x = PosX(a, i, n);
                p.BeginPath();
                p.MoveTo(new Vector2(x, PosY(a, puntos[i].minimo)));
                p.LineTo(new Vector2(x, PosY(a, puntos[i].maximo)));
                p.Stroke();
            }

            // 7) Puntos (encima de todo)
            for (int i = 0; i < n; i++)
            {
                if (!puntos[i].hayDatos) continue;
                Vector2 c = new Vector2(PosX(a, i, n), PosY(a, puntos[i].promedio));

                p.fillColor = ColorFondoPunto;
                p.BeginPath();
                p.Arc(c, RadioPunto + 2f, Angle.Degrees(0), Angle.Degrees(360));
                p.Fill();

                p.fillColor = ColorPorValor(puntos[i].promedio);
                p.BeginPath();
                p.Arc(c, RadioPunto, Angle.Degrees(0), Angle.Degrees(360));
                p.Fill();

                if (i == seleccionado)
                {
                    p.lineWidth = 1.5f;
                    p.strokeColor = Color.white;
                    p.BeginPath();
                    p.Arc(c, RadioPunto + 6f, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                }
            }
        }

        static void Banda(Painter2D p, Rect a, float desde, float hasta, Color c)
        {
            float yArriba = PosY(a, hasta);
            float yAbajo = PosY(a, desde);
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(new Vector2(a.xMin, yArriba));
            p.LineTo(new Vector2(a.xMax, yArriba));
            p.LineTo(new Vector2(a.xMax, yAbajo));
            p.LineTo(new Vector2(a.xMin, yAbajo));
            p.ClosePath();
            p.Fill();
        }

        // =========================================================
        //  Textos (Painter2D no dibuja texto, por eso son Labels)
        // =========================================================
        void ReconstruirTextos()
        {
            foreach (Label t in textos) t.RemoveFromHierarchy();
            textos.Clear();

            Rect a = AreaPlot();
            if (a.width < 10f || a.height < 10f) return;

            CrearTexto("Eficacia (%)", 0f, 4f, 140f, 16f, TextAnchor.MiddleLeft, true);

            for (int v = 0; v <= 100; v += 20)
                CrearTexto(v.ToString(), 0f, PosY(a, v) - 7f, MargenIzq - 6f, 14f, TextAnchor.MiddleRight, false);

            int n = puntos.Length;
            if (n > 0)
            {
                float ranura = a.width / n;
                int paso = Mathf.Max(1, Mathf.CeilToInt(40f / ranura));
                for (int i = 0; i < n; i++)
                {
                    if ((n - 1 - i) % paso != 0) continue;   // siempre se muestra la última
                    float x = PosX(a, i, n);
                    CrearTexto(puntos[i].etiqueta, x - 24f, a.yMax + 6f, 48f, 14f, TextAnchor.UpperCenter, false);
                    CrearTexto(puntos[i].diaSemana, x - 24f, a.yMax + 20f, 48f, 14f, TextAnchor.UpperCenter, false);
                }
            }

            CrearTexto("Tiempo (días)", a.xMin, a.yMax + 36f, a.width, 14f, TextAnchor.UpperCenter, true);

            tooltip.BringToFront();
        }

        void CrearTexto(string texto, float x, float y, float w, float h, TextAnchor alineacion, bool negrita)
        {
            Label l = new Label(texto);
            l.pickingMode = PickingMode.Ignore;
            l.style.position = Position.Absolute;
            l.style.left = x;
            l.style.top = y;
            l.style.width = w;
            l.style.height = h;
            l.style.marginLeft = 0;
            l.style.marginRight = 0;
            l.style.marginTop = 0;
            l.style.marginBottom = 0;
            l.style.paddingLeft = 0;
            l.style.paddingRight = 0;
            l.style.paddingTop = 0;
            l.style.paddingBottom = 0;
            l.style.fontSize = 10;
            l.style.color = ColorTexto;
            l.style.unityTextAlign = alineacion;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            if (negrita) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(l);
            textos.Add(l);
        }

        // =========================================================
        //  Interacción
        // =========================================================
        int IndiceEn(Vector2 local)
        {
            int n = puntos.Length;
            if (n == 0) return -1;
            Rect a = AreaPlot();
            if (local.x < a.xMin || local.x > a.xMax || local.y < a.yMin - 10f || local.y > a.yMax + 10f) return -1;
            int i = Mathf.FloorToInt((local.x - a.xMin) / (a.width / n));
            if (i < 0 || i >= n) return -1;
            return puntos[i].hayDatos ? i : -1;
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            int i = IndiceEn(evt.localPosition);
            if (i < 0)
            {
                tooltip.style.display = DisplayStyle.None;
                return;
            }

            Punto pt = puntos[i];
            Rect a = AreaPlot();
            float x = PosX(a, i, puntos.Length);
            float y = PosY(a, pt.promedio);

            tooltip.text = pt.titulo + "\n" +
                           "Promedio " + pt.promedio.ToString("0") + "% · " + pt.cantidad + " sesiones\n" +
                           "Rango " + pt.minimo.ToString("0") + " a " + pt.maximo.ToString("0") + "%";
            tooltip.style.left = Mathf.Clamp(x, 80f, Mathf.Max(80f, contentRect.width - 80f));

            bool abajo = y < 90f;   // si no cabe arriba del punto, va debajo
            tooltip.style.top = abajo ? y + 14f : y - 14f;
            tooltip.style.translate = new Translate(Length.Percent(-50), Length.Percent(abajo ? 0 : -100));
            tooltip.style.display = DisplayStyle.Flex;
        }

        void OnClick(ClickEvent evt)
        {
            int i = IndiceEn(evt.localPosition);
            if (i < 0) return;
            seleccionado = i;
            MarkDirtyRepaint();
            if (PuntoSeleccionado != null) PuntoSeleccionado(i);
        }
    }

