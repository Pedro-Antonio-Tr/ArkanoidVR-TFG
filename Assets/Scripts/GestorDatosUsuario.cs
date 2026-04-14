using System;
using System.IO;
using UnityEngine;

[Serializable]
public class DatosConfiguracion
{
    public float volumen = 0.5f;
    public int modoMando = 1;
    public int dificultad = 1;
    public float inclinacionPantallaX = 0f;

    public bool pantallaCurva = false;

    // Calibración Brazo IZQUIERDO
    public float centroX_L = 0f;
    public float alcanceIzqX_L = -0.2f;
    public float alcanceDerX_L = 0.2f;

    // Calibración Brazo DERECHO
    public float centroX_R = 0f;
    public float alcanceIzqX_R = -0.2f;
    public float alcanceDerX_R = 0.2f;

    public float distanciaMenu = 1.8f;
    public float distanciaPlana = 3.7f;
    public float distanciaCurva = 2.0f;
}

public class GestorDatosUsuario : MonoBehaviour
{
    public static GestorDatosUsuario Instancia;

    [Header("Usuario Actual")]
    public string idUsuario = "Invitado";
    public DatosConfiguracion configActual = new DatosConfiguracion();

    public string RutaUsuario => Path.Combine(Application.persistentDataPath, idUsuario);
    public string RutaTracking => Path.Combine(RutaUsuario, "Tracking");

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject); // Sobrevive entre escenas
            InicializarCarpetas();
            CargarConfiguracion();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InicializarCarpetas()
    {
        if (!Directory.Exists(RutaUsuario))
        {
            Directory.CreateDirectory(RutaUsuario);
        }
        if (!Directory.Exists(RutaTracking))
        {
            Directory.CreateDirectory(RutaTracking);
        }
    }

    public void GuardarConfiguracion()
    {
        string json = JsonUtility.ToJson(configActual, true);
        File.WriteAllText(Path.Combine(RutaUsuario, "config.json"), json);
    }

    public void CargarConfiguracion()
    {
        string ruta = Path.Combine(RutaUsuario, "config.json");
        if (File.Exists(ruta))
        {
            string json = File.ReadAllText(ruta);
            configActual = JsonUtility.FromJson<DatosConfiguracion>(json);
        }
    }

    public void GuardarPartidaCSV(string nivel, string dificultad, string resultado, int bloques, float fatiga, float reaccion, float duracion, int golpesI, int golpesD)
    {
        string ruta = Path.Combine(RutaUsuario, "historial_partidas.csv");
        bool existe = File.Exists(ruta);

        using (StreamWriter sw = new StreamWriter(ruta, true)) // 'true' es para añadir abajo, no borrar
        {
            if (!existe)
            {
                // Cabecera la primera vez que se crea el archivo del paciente
                sw.WriteLine("FechaHora,Nivel,Dificultad,Duracion(s),Resultado,BloquesRestantes,IndiceFatiga,ReaccionMedia(s),Golpes_IZQ,Golpes_DER");
            }
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{nivel},{dificultad},{duracion:F1},{resultado},{bloques},{fatiga:F2},{reaccion:F2},{golpesI},{golpesD}");
        }
        Debug.Log("Partida guardada en Historial de: " + idUsuario);
    }
}