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
    private string subRutaSesion = ""; // Para el timestamp si es invitado
    public DatosConfiguracion configActual = new DatosConfiguracion();

    public string RutaUsuario
    {
        get
        {
            string baseRuta = Path.Combine(Application.persistentDataPath, idUsuario);
            // Si es invitado, añadimos la subcarpeta de la sesión
            if (idUsuario == "Invitado" && !string.IsNullOrEmpty(subRutaSesion))
            {
                return Path.Combine(baseRuta, subRutaSesion);
            }
            return baseRuta;
        }
    }
    public string RutaTracking => Path.Combine(RutaUsuario, "Tracking");

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            CapturarIDDesdeIntent();

            if (idUsuario == "Invitado")
            {
                subRutaSesion = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

            InicializarCarpetas();
            CargarConfiguracion();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void CapturarIDDesdeIntent()
    {
        // Solo intentamos esto en Android (Gafas), en el Editor fallaría
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (currentActivity != null)
                {
                    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
                    if (intent != null)
                    {
                        // Buscamos el extra "user"
                        using (AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras"))
                        {
                            if (extras != null)
                            {
                                string idCapturado = extras.Call<string>("getString", "user");
                                if (!string.IsNullOrEmpty(idCapturado))
                                {
                                    idUsuario = idCapturado;
                                    Debug.Log("ID de Usuario capturado con éxito: " + idUsuario);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("No se detectó parámetro de usuario en el Intent, usando Invitado. " + e.Message);
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

        // Si no existe en la sesión actual (invitado), intentamos buscar una global en la raíz de Invitado
        // para que no pierdan el volumen/ajustes cada vez que abren la app si son invitados.
        if (!File.Exists(ruta) && idUsuario == "Invitado")
        {
            ruta = Path.Combine(Application.persistentDataPath, "Invitado", "config.json");
        }

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

        using (StreamWriter sw = new StreamWriter(ruta, true))
        {
            if (!existe)
            {
                sw.WriteLine("FechaHora;Nivel;Dificultad;Duracion(s);Resultado;BloquesRestantes;IndiceFatiga;ReaccionMedia(s);Golpes_IZQ;Golpes_DER");
            }
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{nivel};{dificultad};{duracion:F1};{resultado};{bloques};{fatiga:F2};{reaccion:F2};{golpesI};{golpesD}");
        }
    }
}