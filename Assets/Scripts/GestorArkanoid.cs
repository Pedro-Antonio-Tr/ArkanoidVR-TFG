using UnityEngine;
using TMPro;
using System.Collections;

public class GestorArkanoid : MonoBehaviour
{
    public static GestorArkanoid Instancia;

    [Header("Selector de Niveles")]
    public GameObject[] listaDeNiveles;
    public int nivelElegido = 0;
    private GameObject nivelActualInstanciado;

    [Header("Textos de Pantalla (Holograma)")]
    public TextMeshProUGUI textoMensajes;
    public TextMeshProUGUI textoEstadisticas; // Ahora lo usaremos para el Tiempo
    public TextMeshProUGUI textoDebug;

    [Header("Conexión Clínica")]
    public ControladorPalaVR controladorPala;

    [Header("Aparición de Pelota")]
    public GameObject prefabPelota;
    public Transform puntoAparicion;

    [Header("Estado de la Partida")]
    public int pelotasEnJuego = 0;
    public int bloquesRestantes = 0;
    public bool juegoEmpezado = false;

    [Header("Sonidos Interfaz")]
    public AudioClip sonidoCuentaAtras;
    public AudioClip sonidoGo;
    private AudioSource audioSourceUI; // El reproductor
    public AudioClip sonidoVictoria;
    public AudioClip sonidoDerrota;

    [Header("Mejora Explosiva")]
    public bool explosivoActivo = false;
    public float duracionExplosivo = 5f;
    public float tiempoExplosivoRestante = 0f;
    public AudioClip sonidoExplosion;

    // VARIABLES DE TIEMPO
    private float tiempoPartida = 0f;
    private bool cronometroActivo = false;
    public bool enCuentaAtras = false;
    private Coroutine rutinaReanudacion;
    private bool yaTerminado = false;
    private string logRNG = "---";

    void Awake() { Instancia = this; }

    void Start()
    {
        textoMensajes.text = "ABRE EL MENÚ PARA EMPEZAR";
        textoEstadisticas.text = "";
        audioSourceUI = gameObject.AddComponent<AudioSource>();
        audioSourceUI.playOnAwake = false;
        if (textoDebug != null)
        {
            textoDebug.text = "";
        }
        CargarPrevisualizacion(0); // Cargamos el Nivel 1 de fondo
    }

    void Update()
    {
        if (cronometroActivo)
        {
            tiempoPartida += Time.deltaTime;

            int vidaTotal = 0;
            GameObject[] bloques = GameObject.FindGameObjectsWithTag("Bloque");
            bloquesRestantes = bloques.Length;

            foreach (GameObject b in bloques)
            {
                BloqueArkanoid scriptBloque = b.GetComponent<BloqueArkanoid>();
                if (scriptBloque != null)
                {
                    vidaTotal += scriptBloque.puntosDeVida;
                }
            }

            if (bloquesRestantes <= 0 || vidaTotal <= 0)
            {
                LimpiarPelotas();
                TerminarPartida("¡NIVEL COMPLETADO!");
            }
        }

        if (explosivoActivo)
        {
            tiempoExplosivoRestante -= Time.deltaTime;
            if (tiempoExplosivoRestante <= 0)
            {
                explosivoActivo = false;
            }
        }

        if (juegoEmpezado || enCuentaAtras)
        {
            ActualizarDebug();
        }
    }

    public void CargarPrevisualizacion(int indice)
    {
        if (nivelActualInstanciado != null)
        {
            Destroy(nivelActualInstanciado);
        }

        nivelElegido = indice;
        if (listaDeNiveles.Length > 0)
        {
            nivelActualInstanciado = Instantiate(listaDeNiveles[nivelElegido], transform.position, Quaternion.identity, transform);
        }
    }

    public void EmpezarPartidaDesdeMenu()
    {
        LimpiarPelotas();
        textoMensajes.text = "";
        textoEstadisticas.text = "";
        tiempoPartida = 0f;
        StartCoroutine(RutinaInicioPartida());
    }

    public void ReiniciarNivelActual()
    {
        Time.timeScale = 1f; // Por si venimos de la pausa
        LimpiarPelotas();
        EmpezarPartidaDesdeMenu();
    }

    public void VolverAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        juegoEmpezado = false;
        enCuentaAtras = false;
        cronometroActivo = false;
        LimpiarPelotas();
        textoMensajes.text = "ABRE EL MENÚ PARA EMPEZAR";
        textoEstadisticas.text = "";
        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.DetenerTelemetria();
        }
        if (textoDebug != null)
        {
            textoDebug.text = "";
        }
        CargarPrevisualizacion(nivelElegido);
    }

    IEnumerator RutinaInicioPartida()
    {
        enCuentaAtras = true;
        juegoEmpezado = false;
        cronometroActivo = false;
        yaTerminado = false;
        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.ReiniciarContadoresLateralidad();
        }

        CargarPrevisualizacion(nivelElegido);

        yield return new WaitForSeconds(0.1f);
        bloquesRestantes = GameObject.FindGameObjectsWithTag("Bloque").Length;

        textoMensajes.text = "3";
        if (sonidoCuentaAtras != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "2";
        if (sonidoCuentaAtras != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "1";
        if (sonidoCuentaAtras != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "¡GO!";
        if (sonidoGo != null)
        {
            audioSourceUI.PlayOneShot(sonidoGo);
        }

        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.IniciarTelemetria($"Nivel_{nivelElegido + 1}");
        }

        Instantiate(prefabPelota, puntoAparicion.position, Quaternion.identity, transform);

        enCuentaAtras = false;
        juegoEmpezado = true;
        cronometroActivo = true;

        yield return new WaitForSeconds(1f);
        if (textoMensajes.text == "¡GO!")
        {
            textoMensajes.text = "";
        }
        enCuentaAtras = false;
    }

    public void RegistrarPelota() { pelotasEnJuego++; }

    public void PelotaDestruida()
    {
        pelotasEnJuego--;
        if (pelotasEnJuego <= 0 && bloquesRestantes > 0) TerminarPartida("¡FIN DEL JUEGO!");
    }

    public void BloqueDestruido()
    {
        GameObject[] bloques = GameObject.FindGameObjectsWithTag("Bloque");
        bloquesRestantes = bloques.Length;
        if (bloquesRestantes <= 0)
        {
            LimpiarPelotas();
            TerminarPartida("¡NIVEL COMPLETADO!");
        }
    }

    void TerminarPartida(string mensaje)
    {
        if (yaTerminado) return;
        yaTerminado = true;
        cronometroActivo = false;
        juegoEmpezado = false;
        textoMensajes.text = mensaje;
        explosivoActivo = false;

        // Formatear tiempo en Minutos:Segundos
        int minutos = Mathf.FloorToInt(tiempoPartida / 60F);
        int segundos = Mathf.FloorToInt(tiempoPartida - minutos * 60);
        textoEstadisticas.text = $"TIEMPO: {string.Format("{0:00}:{1:00}", minutos, segundos)}";
        string resultado = "Derrota";
        if (mensaje.Contains("COMPLETADO"))
        {
            ReproducirSonidoGlobal(sonidoVictoria);
            resultado = "Victoria";
        }
        else if (mensaje.Contains("ABANDONO") || mensaje.Contains("MENU"))
        {
            resultado = "Abortado"; // Para cuando le da a Volver
        }
        else
        {
            ReproducirSonidoGlobal(sonidoDerrota);
        }

        if (GestorDatosUsuario.Instancia != null && MonitorClinico.Instancia != null)
        {
            string diff = GestorDatosUsuario.Instancia.configActual.dificultad.ToString();
            float fatiga = MonitorClinico.Instancia.indiceFatiga;
            float reaccion = MonitorClinico.Instancia.ObtenerMediaReaccion();
            int golpesI = MonitorClinico.Instancia.golpesIzquierda;
            int golpesD = MonitorClinico.Instancia.golpesDerecha;
            MonitorClinico.Instancia.DetenerTelemetria();

            GestorDatosUsuario.Instancia.GuardarPartidaCSV(
                $"Nivel {nivelElegido + 1}",
                diff,
                resultado,
                bloquesRestantes,
                fatiga,
                reaccion,
                tiempoPartida,
                golpesI,
                golpesD
            );
        }
        ControladorMenu.Instancia.MostrarResultadosFinales(mensaje);
    }

    void LimpiarPelotas()
    {
        GameObject[] pelotas = GameObject.FindGameObjectsWithTag("Pelota");
        foreach (GameObject p in pelotas) Destroy(p);
        pelotasEnJuego = 0;
    }

    public void AlternarPausa(bool estaEnPausa)
    {
        if (!juegoEmpezado && !enCuentaAtras) return;

        if (rutinaReanudacion != null) StopCoroutine(rutinaReanudacion);

        if (estaEnPausa)
        {
            Time.timeScale = 0f;
            cronometroActivo = false;
            // No pisamos el número de la cuenta atrás si está en proceso
            if (!enCuentaAtras) textoMensajes.text = "PAUSA";
        }
        else
        {
            if (enCuentaAtras)
            {
                // Si estábamos en cuenta atrás, solo devolvemos el tiempo a la normalidad
                Time.timeScale = 1f;
            }
            else
            {
                rutinaReanudacion = StartCoroutine(RutinaDescongelarTiempo());
            }
        }
    }

    IEnumerator RutinaDescongelarTiempo()
    {
        textoMensajes.text = "REANUDANDO... 3";
        yield return new WaitForSecondsRealtime(1f);
        textoMensajes.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        textoMensajes.text = "1";
        yield return new WaitForSecondsRealtime(1f);
        textoMensajes.text = "";

        Time.timeScale = 1f;
        cronometroActivo = true;
    }

    public void DuplicarPelotas()
    {
        GameObject[] pelotasActuales = GameObject.FindGameObjectsWithTag("Pelota");

        foreach (GameObject pelota in pelotasActuales)
        {
            GameObject nuevaPelota = Instantiate(prefabPelota, pelota.transform.position, Quaternion.identity, transform);

            Rigidbody rbOriginal = pelota.GetComponent<Rigidbody>();
            Rigidbody rbNueva = nuevaPelota.GetComponent<Rigidbody>();

            if (rbOriginal != null && rbNueva != null)
            {
                // Invertimos su velocidad en el eje X para que salgan en "V"
                Vector3 velOriginal = rbOriginal.linearVelocity;
                rbNueva.linearVelocity = new Vector3(-velOriginal.x, velOriginal.y, 0f);
            }
        }
    }

    void ActualizarDebug()
    {
        if (textoDebug == null) return;

        int vidaTotal = 0;
        GameObject[] bloques = GameObject.FindGameObjectsWithTag("Bloque");

        foreach (GameObject b in bloques)
        {
            BloqueArkanoid scriptBloque = b.GetComponent<BloqueArkanoid>();
            if (scriptBloque != null)
            {
                vidaTotal += scriptBloque.puntosDeVida;
            }
        }

        textoDebug.text = $"Pelotas restantes: {pelotasEnJuego} | Bloques: {bloques.Length} | Vida Total: {vidaTotal} | {logRNG}";
    }

    public void ReproducirSonidoGlobal(AudioClip clip)
    {
        if (clip != null)
        {
            audioSourceUI.PlayOneShot(clip);
        }
    }

    public string ObtenerTiempoFormateado()
    {
        int minutos = Mathf.FloorToInt(tiempoPartida / 60F);
        int segundos = Mathf.FloorToInt(tiempoPartida - minutos * 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    public int ObtenerVidaTotalBloques()
    {
        int vidaTotal = 0;
        GameObject[] bloques = GameObject.FindGameObjectsWithTag("Bloque");

        foreach (GameObject b in bloques)
        {
            BloqueArkanoid scriptBloque = b.GetComponent<BloqueArkanoid>();
            if (scriptBloque != null) vidaTotal += scriptBloque.puntosDeVida;
        }
        return vidaTotal;
    }

    public void ActivarExplosivo()
    {
        explosivoActivo = true;
        tiempoExplosivoRestante = duracionExplosivo;
        // Cambiamos el material de las pelotas a explosivo
        GameObject[] pelotas = GameObject.FindGameObjectsWithTag("Pelota");
        foreach (GameObject p in pelotas)
        {
            ComportamientoPelota scriptPelota = p.GetComponent<ComportamientoPelota>();
            if (scriptPelota != null)
            {
                scriptPelota.ActualizarVisualesExplosivos();
            }
        }
    }

    public void ActualizarTextoRNG(string nuevoLog)
    {
        logRNG = nuevoLog;
    }
}