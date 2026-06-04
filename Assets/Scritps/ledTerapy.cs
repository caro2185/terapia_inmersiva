using UnityEngine;
using UnityEngine.UIElements;

public class ledTerapy : MonoBehaviour
{
    private VisualElement panelDerecha;
    private Button botonTerapias;
    private Button botonVolver;

    private void OnEnable()
    {
        // 1. Conseguimos la raíz de la interfaz
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        // 2. Buscamos solo los 3 elementos necesarios para la prueba
        botonTerapias = root.Q<Button>("terapias");
        panelDerecha = root.Q<VisualElement>("derecha");
        botonVolver = root.Q<Button>("volver");

        // 3. Asignamos los clics de forma directa y simplificada
        botonTerapias.clicked += AbrirPanel;
        botonVolver.clicked += CerrarPanel;
    }

    private void OnDisable()
    {
        // Buena práctica: desvincular los eventos
        botonTerapias.clicked -= AbrirPanel;
        botonVolver.clicked -= CerrarPanel;
    }

    private void AbrirPanel()
    {
        Debug.Log("-> CLIC EN TERAPIAS: Forzando Translate a 0%");

        // Esto modifica el valor directamente en vivo, saltándose el USS
        panelDerecha.style.translate = new StyleTranslate(new Translate(Length.Percent(0), 0));
    }

    private void CerrarPanel()
    {
        Debug.Log("-> CLIC EN VOLVER: Forzando Translate a 100%");

        // Esto vuelve a mandar el panel al 100% hacia la derecha
        panelDerecha.style.translate = new StyleTranslate(new Translate(Length.Percent(100), 0));
    }
}