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
    public NivelDificultad dificultadActual = NivelDificultad.Facil;

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

    [Header("Tiempo de Reacción")]
    public float margenPala = 0.5f; // Ajusta esto a la mitad del ancho de tu pala
    public float tiempoMedioReaccion = 0f;
    private int conteoTiemposReaccion = 0;
    private float sumaTiemposReaccion = 0f;

    private float tiempoInicioEstimulo = 0f;
    private bool estimuloActivo = false;
    private float posicionDestinoX = 0f;

    private StreamWriter escritorTelemetria;
    private bool grabandoTelemetria = false;
    private float tiempoInicioSesionTelemetria;

    // Variables internas para fatiga
    private Quaternion rotacionAnteriorIzq;
    private Quaternion rotacionAnteriorDer;

    public string idSesionActual = "";

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

    public float ObtenerMediaReaccion()
    {
        if (conteoTiemposReaccion == 0) return 0f;
        return sumaTiemposReaccion / conteoTiemposReaccion;
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
        idSesionActual = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string nombreArchivo = $"Telemetria_{GestorDatosUsuario.Instancia.idUsuario}_{nombreNivel}_{idSesionActual}.csv";
        string ruta = Path.Combine(GestorDatosUsuario.Instancia.RutaTracking, nombreArchivo);

        conteoTiemposReaccion = 0;
        sumaTiemposReaccion = 0f;
        estimuloActivo = false;

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

    public void IniciarMedicionReaccion(float destinoX)
    {
        float margenDinamico = ObtenerMargenDinamico();

        if (ControladorPalaVR.Instancia != null && ControladorPalaVR.Instancia.AlgunaPalaEnPosicion(destinoX, margenDinamico))
        {
            return;
        }

        posicionDestinoX = destinoX;
        tiempoInicioEstimulo = Time.time;
        estimuloActivo = true;
    }

    public void ComprobarLlegadaPala(float posXActualPala)
    {
        if (!estimuloActivo) return;

        if (Mathf.Abs(posXActualPala - posicionDestinoX) <= ObtenerMargenDinamico())
        {
            float tiempoReaccion = Time.time - tiempoInicioEstimulo;
            sumaTiemposReaccion += tiempoReaccion;
            conteoTiemposReaccion++;
            estimuloActivo = false;
        }
    }

    // Calcula el margen según la dificultad
    private float ObtenerMargenDinamico()
    {
        float escalaBase = 3f;
        if (dificultadActual == NivelDificultad.Facil) escalaBase *= 1.5f;
        else if (dificultadActual == NivelDificultad.Dificil) escalaBase *= 0.75f;

        return (escalaBase / 2f) + 0.5f;
    }

    void OnDestroy()
    {
        DetenerTelemetria();
    }
}