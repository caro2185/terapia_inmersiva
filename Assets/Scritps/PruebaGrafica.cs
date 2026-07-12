using UnityEngine;
using UnityEngine.UIElements;

public class PruebaGrafica : MonoBehaviour
{
    private VisualElement panelReportes;
    private VisualElement miPunto;
    private Button btnVerReportes;
    private Button btnCerrarReportes;

    void OnEnable()
    {
        // 1. Obtenemos la raíz de la interfaz (UXML)
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 2. Buscamos los elementos por su ID (#) exacto
        panelReportes = root.Q<VisualElement>("panel-reportes");
        miPunto = root.Q<VisualElement>("mi-punto-discreto");
        btnVerReportes = root.Q<Button>("btn-ver-reportes");
        btnCerrarReportes = root.Q<Button>("btn-cerrar-reportes");

        // 3. Asignamos los eventos de los botones con expresiones lambda compactas
        if (btnVerReportes != null)
        {
            btnVerReportes.clicked += AbrirYGraficar;
        }

        if (btnCerrarReportes != null)
        {
            btnCerrarReportes.clicked += () => panelReportes.style.display = DisplayStyle.None;
        }
    }

    private void AbrirYGraficar()
    {
        if (panelReportes == null || miPunto == null) return;

        // Mostramos el contenedor de reportes
        panelReportes.style.display = DisplayStyle.Flex;

        // SIMULACIÓN MVP: Le asignamos un 75% de rendimiento al punto
        float rendimientoSimulado = 75f;

        // Modificamos la propiedad bottom usando porcentajes nativos
        miPunto.style.bottom = Length.Percent(rendimientoSimulado);
    }
}