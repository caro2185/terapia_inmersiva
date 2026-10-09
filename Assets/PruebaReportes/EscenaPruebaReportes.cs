using UnityEngine;
using UnityEngine.UIElements;


    /// <summary>
    /// Escena de prueba del informe de reportes.
    /// Va en el mismo GameObject que el UIDocument (que usa PruebaReportes.uxml).
    /// Lee el JSON que arrastres al Inspector y se lo pasa a ReportesPaciente.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EscenaPruebaReportes : MonoBehaviour
    {
        [Tooltip("Arrastra aquí sesiones_prueba.json")]
        [SerializeField] private TextAsset archivoJson;

        [Tooltip("Opcional. Arrastra PruebaReportes.uss solo si la pantalla se ve sin estilos.")]
        [SerializeField] private StyleSheet hojaDeEstilos;

        private ReportesPaciente reportes;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            if (hojaDeEstilos != null && !root.styleSheets.Contains(hojaDeEstilos))
                root.styleSheets.Add(hojaDeEstilos);

            reportes = new ReportesPaciente(root);

            if (archivoJson == null)
            {
                Debug.LogError("EscenaPruebaReportes: arrastra sesiones_prueba.json al campo 'Archivo Json' del Inspector.");
                return;
            }

            SesionesWrapper datos = JsonUtility.FromJson<SesionesWrapper>(archivoJson.text);
            if (datos == null || datos.sesiones == null || datos.sesiones.Length == 0)
            {
                Debug.LogError("EscenaPruebaReportes: el JSON no trae sesiones. Debe empezar con {\"sesiones\":[ ... ]}");
                return;
            }

            Debug.Log("EscenaPruebaReportes: sesiones cargadas = " + datos.sesiones.Length);
            reportes.Mostrar(datos.sesiones);
        }
    }

