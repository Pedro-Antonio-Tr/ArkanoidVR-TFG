using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MonitorClinico : MonoBehaviour
{
    public static MonitorClinico Instancia;

    public enum ModoControl { Izquierdo, Derecho, Ambos }
    public enum NivelDificultad { Facil, Normal, Dificil }

    [Header("Ajustes de Dificultad")]
    public NivelDificultad dificultadActual = NivelDificultad.Normal;

    [Header("Configuración Actual")]
    public ModoControl modoActual = ModoControl.Derecho;

    [Header("Referencias (Trackers)")]
    public Transform mandoIzquierdo;
    public Transform mandoDerecho;

    [Header("Métricas Recopiladas")]
    public float tiempoMandoIzquierdo = 0f;
    public float tiempoMandoDerecho = 0f;
    public float tiempoAmbosMandos = 0f;
    public float indiceFatiga = 0f; // Acumulación de micro-temblores

    [Header("Registro de golpes con cada mano")]
    public int golpesIzquierda = 0;
    public int golpesDerecha = 0;

    [Header("Telemetría (Tracking Raw)")]
    public Transform headAnchor;
    public float frecuenciaRegistro = 0.1f; // 0.1s = 10 registros por segundo
    public float umbralMovimientoBrusco = 3.0f; // Se considera brusco si la aceleración supera 3 m/s²

    private StreamWriter escritorTelemetria;
    private bool grabandoTelemetria = false;
    private float tiempoInicioSesionTelemetria;

    // Tiempos de reacción
    private List<float> tiemposDeReaccion = new List<float>();
    private bool midiendoReaccion = false;
    private float tiempoInicioReaccion = 0f;

    // Variables internas para fatiga
    private Quaternion rotacionAnteriorIzq;
    private Quaternion rotacionAnteriorDer;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
    }

    void Start()
    {
        if (mandoIzquierdo != null) rotacionAnteriorIzq = mandoIzquierdo.rotation;
        if (mandoDerecho != null) rotacionAnteriorDer = mandoDerecho.rotation;
    }

    void Update()
    {
        // Solo registramos si el juego está activo
        if (GestorArkanoid.Instancia != null && !GestorArkanoid.Instancia.juegoEmpezado) return;
        if (Time.timeScale == 0) return; // Si está en pausa, no medimos

        RegistrarTiempoUso();
        MedirFatiga();
        ComprobarReaccion();
    }

    void RegistrarTiempoUso()
    {
        switch (modoActual)
        {
            case ModoControl.Izquierdo: tiempoMandoIzquierdo += Time.deltaTime; break;
            case ModoControl.Derecho: tiempoMandoDerecho += Time.deltaTime; break;
            case ModoControl.Ambos: tiempoAmbosMandos += Time.deltaTime; break;
        }
    }

    void MedirFatiga()
    {
        // Medimos cuánto ha rotado el mando en este frame (el temblor se nota mucho en la muñeca)
        if (modoActual == ModoControl.Izquierdo || modoActual == ModoControl.Ambos)
        {
            float deltaRotIzq = Quaternion.Angle(rotacionAnteriorIzq, mandoIzquierdo.rotation);
            indiceFatiga += deltaRotIzq;
            rotacionAnteriorIzq = mandoIzquierdo.rotation;
        }

        if (modoActual == ModoControl.Derecho || modoActual == ModoControl.Ambos)
        {
            float deltaRotDer = Quaternion.Angle(rotacionAnteriorDer, mandoDerecho.rotation);
            indiceFatiga += deltaRotDer;
            rotacionAnteriorDer = mandoDerecho.rotation;
        }
    }

    // Esta función la llamará la pelota cuando choque contra un bloque
    public void IniciarMedicionReaccion()
    {
        if (!midiendoReaccion)
        {
            midiendoReaccion = true;
            tiempoInicioReaccion = Time.time;
        }
    }

    void ComprobarReaccion()
    {
        if (midiendoReaccion)
        {
            float velocidadMovimiento = 0f;

            // Leemos la velocidad real de la mano en el eje X usando la API de Meta
            if (modoActual == ModoControl.Derecho || modoActual == ModoControl.Ambos)
            {
                velocidadMovimiento = Mathf.Abs(OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch).x);
            }
            else if (modoActual == ModoControl.Izquierdo)
            {
                velocidadMovimiento = Mathf.Abs(OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch).x);
            }

            // Si la mano se mueve a una velocidad significativa (0.2 metros/segundo), ha reaccionado
            if (velocidadMovimiento > 0.2f)
            {
                float tiempoReaccion = Time.time - tiempoInicioReaccion;
                tiemposDeReaccion.Add(tiempoReaccion);
                midiendoReaccion = false;
            }
        }
    }

    public void GuardarDatosCSV()
    {
        // Ruta persistente en Android (Meta Quest)
        string fechaHora = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string nombreArchivo = $"Sesion_{fechaHora}.csv";
        string ruta = Path.Combine(Application.persistentDataPath, nombreArchivo);

        // Calcular media de reacción
        float sumaReaccion = 0f;
        foreach (float t in tiemposDeReaccion) sumaReaccion += t;
        float mediaReaccion = tiemposDeReaccion.Count > 0 ? sumaReaccion / tiemposDeReaccion.Count : 0f;

        try
        {
            using (StreamWriter writer = new StreamWriter(ruta, false))
            {
                // Cabeceras del CSV
                writer.WriteLine("Fecha,Modo_Mando_Derecho,Modo_Mando_Izquierdo,Modo_Ambos_Mandos,Dificultad,Indice_Fatiga,Reaccion_Media");

                // Datos
                writer.WriteLine($"{fechaHora},{tiempoMandoDerecho:F2},{tiempoMandoIzquierdo:F2},{tiempoAmbosMandos:F2},{dificultadActual},{indiceFatiga:F2},{mediaReaccion:F2}");
            }
            Debug.Log("¡CSV Guardado con éxito en: " + ruta + "!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar el CSV: " + e.Message);
        }
    }

    public float ObtenerMediaReaccion()
    {
        if (tiemposDeReaccion == null || tiemposDeReaccion.Count == 0)
        {
            return 0f;
        }

        float suma = 0f;
        foreach (float t in tiemposDeReaccion)
        {
            suma += t;
        }

        return suma / tiemposDeReaccion.Count;
    }

    public void RegistrarGolpePala(bool esIzquierda)
    {
        if (esIzquierda) golpesIzquierda++;
        else golpesDerecha++;
    }

    public void ReiniciarContadoresLateralidad()
    {
        golpesIzquierda = 0;
        golpesDerecha = 0;
    }

    public void IniciarTelemetria(string nombreNivel)
    {
        // Creamos un archivo único para esta partida
        string fechaHora = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string nombreArchivo = $"Telemetria_{GestorDatosUsuario.Instancia.idUsuario}_{nombreNivel}_{fechaHora}.csv";
        string ruta = Path.Combine(GestorDatosUsuario.Instancia.RutaTracking, nombreArchivo);

        try
        {
            escritorTelemetria = new StreamWriter(ruta, false);

            escritorTelemetria.WriteLine("Tiempo(s);Head_RotX;Head_RotY;Head_RotZ;L_PosX;L_PosY;L_PosZ;L_Vel(m/s);R_PosX;R_PosY;R_PosZ;R_Vel(m/s);Evento");

            tiempoInicioSesionTelemetria = Time.time;
            grabandoTelemetria = true;
            StartCoroutine(RutinaRegistroTelemetria());

            Debug.Log("Telemetría iniciada en: " + ruta);
        }
        catch (Exception e)
        {
            Debug.LogError("Error al crear archivo de telemetría: " + e.Message);
        }
    }

    public void DetenerTelemetria()
    {
        grabandoTelemetria = false;
        if (escritorTelemetria != null)
        {
            escritorTelemetria.Close();
            escritorTelemetria = null;
            Debug.Log("Archivo de telemetría cerrado y guardado.");
        }
    }

    private System.Collections.IEnumerator RutinaRegistroTelemetria()
    {
        while (grabandoTelemetria)
        {
            // Solo grabamos si el juego no está en pausa
            if (Time.timeScale > 0)
            {
                float t = Time.time - tiempoInicioSesionTelemetria;

                // Rotación de la cabeza
                Vector3 hR = headAnchor != null ? headAnchor.eulerAngles : Vector3.zero;

                // Posición de las manos
                Vector3 lP = mandoIzquierdo != null ? mandoIzquierdo.localPosition : Vector3.zero;
                Vector3 rP = mandoDerecho != null ? mandoDerecho.localPosition : Vector3.zero;

                // Velocidades de las manos (Magnitud total en metros/segundo)
                float velL = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch).magnitude;
                float velR = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch).magnitude;

                // Detección de Eventos (Movimientos bruscos)
                string evento = "NORMAL";
                if (velL > umbralMovimientoBrusco) evento = "MOVIMIENTO_BRUSCO_IZQ";
                if (velR > umbralMovimientoBrusco) evento = "MOVIMIENTO_BRUSCO_DER";

                // Formateamos la línea con dos decimales (F2) y punto y coma
                string linea = $"{t:F2};{hR.x:F2};{hR.y:F2};{hR.z:F2};{lP.x:F2};{lP.y:F2};{lP.z:F2};{velL:F2};{rP.x:F2};{rP.y:F2};{rP.z:F2};{velR:F2};{evento}";

                if (escritorTelemetria != null)
                {
                    escritorTelemetria.WriteLine(linea);
                }
            }

            yield return new WaitForSeconds(frecuenciaRegistro);
        }
    }

    void OnDestroy()
    {
        DetenerTelemetria();
    }
}