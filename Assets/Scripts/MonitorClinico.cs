using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MonitorClinico : MonoBehaviour
{
    public static MonitorClinico Instancia;

    public enum ModoControl { Izquierdo, Derecho, Ambos }

    [Header("Configuración Actual")]
    public ModoControl modoActual = ModoControl.Derecho;

    [Header("Referencias (Trackers)")]
    public Transform mandoIzquierdo;
    public Transform mandoDerecho;
    public ControladorPalaVR palaVR;

    [Header("Métricas Recopiladas")]
    public float tiempoMandoIzquierdo = 0f;
    public float tiempoMandoDerecho = 0f;
    public float tiempoAmbosMandos = 0f;
    public float indiceFatiga = 0f; // Acumulación de micro-temblores

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
        if (midiendoReaccion && palaVR != null)
        {
            // Si la pala se mueve a una velocidad significativa (ha reaccionado)
            float velocidadPala = Mathf.Abs(palaVR.GetComponent<Rigidbody>().linearVelocity.x);
            if (velocidadPala > 0.5f)
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
                writer.WriteLine("Fecha,Modo_Mando_Derecho,Modo_Mando_Izquierdo,Modo_Ambos_Mandos,Indice_Fatiga,Reaccion_Media");

                // Datos
                writer.WriteLine($"{fechaHora},{tiempoMandoDerecho:F2},{tiempoMandoIzquierdo:F2},{tiempoAmbosMandos:F2},{indiceFatiga:F2},{mediaReaccion:F2}");
            }
            Debug.Log("¡CSV Guardado con éxito en: " + ruta + "!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar el CSV: " + e.Message);
        }
    }
}