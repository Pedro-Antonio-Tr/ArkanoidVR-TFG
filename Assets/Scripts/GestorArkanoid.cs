using UnityEngine;
using TMPro;
using System.Collections;
using System.Threading;

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

    [Header("Menú")]
    public ControladorMenu controladorMenu;

    [Header("Aparición de Pelota")]
    public GameObject prefabPelota;
    public Transform puntoAparicion;

    [Header("Péndulo de Lanzamiento")]
    public Transform pivotFlecha;
    public float anguloMaximo = 45f; // Rango de balanceo (-45 a 45 grados)
    public float velocidadPendulo = 3f; // Qué tan rápido oscila

    private bool animandoPendulo = false;
    private float anguloPenduloActual = 0f;

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

    [Header("Sistema de Vidas")]
    public int vidasFacil = 3;
    public int vidasNormal = 2;
    public int vidasDificil = 1;
    private int vidasActuales;
    public int puntosPorVidaRestante = 500;

    [Header("Puntuación")]
    public int puntuacionActual = 0;
    public int tiempoParSegundos = 300; // 5 minutos para el bonus por tiempo
    public int puntosPorSegundoAhorrado = 25;
    private bool recordSuperado = false;

    [Header("UI Vidas (Corazones)")]
    public RectTransform contenedorCorazones;
    public UnityEngine.UI.Image[] imagenesCorazones;
    public Sprite spriteVidaLlena;
    public Sprite spriteVidaVacia;

    [Header("UI Puntuación")]
    public TextMeshProUGUI textoPuntuacion;

    [Header("Efectos de Texto")]
    public DynamicTextData perfilTextoRecord;

    private int recordDelNivel = 0;

    private Vector3 posicionEsquinaCorazones;
    private Vector3 escalaOriginalCorazones;

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
        textoMensajes.text = "Elige un nivel para comenzar";
        if (textoEstadisticas != null) textoEstadisticas.text = "";
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
        if (animandoPendulo && pivotFlecha != null)
        {
            anguloPenduloActual = Mathf.Sin(Time.time * velocidadPendulo) * anguloMaximo;

            pivotFlecha.localRotation = Quaternion.Euler(0f, 0f, -anguloPenduloActual);
        }

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
        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = "000000";
        }
        nivelElegido = indice;
        if (listaDeNiveles.Length > 0)
        {
            nivelActualInstanciado = Instantiate(listaDeNiveles[nivelElegido], transform.position, Quaternion.identity, transform);
        }
    }

    public void EmpezarPartidaDesdeMenu()
    {
        Time.timeScale = 1f;
        LimpiarPelotas();
        textoMensajes.text = "";
        if (textoEstadisticas != null) textoEstadisticas.text = "";
        tiempoPartida = 0f;
        explosivoActivo = false;
        recordSuperado = false;

        puntuacionActual = 0;
        if (GestorDatosUsuario.Instancia != null)
        {
            recordDelNivel = GestorDatosUsuario.Instancia.ObtenerRecordPorNivel($"Nivel {nivelElegido + 1}");
        }
        ActualizarTextoPuntuacion();
        CargarPrevisualizacion(nivelElegido);
        ConfigurarVidasIniciales();

        StartCoroutine(RutinaInicioPartida());
        if (NotificacionFlotanteVR.Instancia != null)
        {
            NotificacionFlotanteVR.Instancia.MostrarNotificacion($"Puedes pausar la partida en cualquier momento pulsando el botón A o el botón X", 5f);
        }
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
        textoMensajes.text = "Elige un nivel para comenzar";
        if (textoEstadisticas != null) textoEstadisticas.text = "";
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

        yield return new WaitForSeconds(0.1f);
        bloquesRestantes = GameObject.FindGameObjectsWithTag("Bloque").Length;

        if (pivotFlecha != null)
        {
            pivotFlecha.gameObject.SetActive(true);
        }
        animandoPendulo = true;

        textoMensajes.text = "3";
        if (sonidoCuentaAtras != null && audioSourceUI != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "2";
        if (sonidoCuentaAtras != null && audioSourceUI != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "1";
        if (sonidoCuentaAtras != null && audioSourceUI != null)
        {
            audioSourceUI.PlayOneShot(sonidoCuentaAtras);
        }
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "¡GO!";
        if (sonidoGo != null)
        {
            audioSourceUI.PlayOneShot(sonidoGo);
        }

        animandoPendulo = false;
        if (pivotFlecha != null)
        {
            pivotFlecha.gameObject.SetActive(false);
        }

        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.IniciarTelemetria($"Nivel_{nivelElegido + 1}");
        }

        Vector3 direccionLanzamiento = Quaternion.Euler(0f, 0f, -anguloPenduloActual) * Vector3.up;

        GameObject nuevaPelota = Instantiate(prefabPelota, puntoAparicion.position, Quaternion.identity, transform);
        ComportamientoPelota scriptPelota = nuevaPelota.GetComponent<ComportamientoPelota>();

        if (scriptPelota != null)
        {
            scriptPelota.Lanzar(direccionLanzamiento);
        }

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
        if (pelotasEnJuego <= 0 && bloquesRestantes > 0)
        {
            StartCoroutine(RutinaPerderVida());
        }
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
        recordSuperado = false;

        // Formatear tiempo en Minutos:Segundos
        int minutos = Mathf.FloorToInt(tiempoPartida / 60F);
        int segundos = Mathf.FloorToInt(tiempoPartida - minutos * 60);
        string tiempoFormat = string.Format("{0:00}:{1:00}", minutos, segundos);

        int bonusTiempo = 0;
        int bonusVidas = 0;

        string resultado = "Derrota";
        if (mensaje.Contains("COMPLETADO"))
        {
            bonusVidas = vidasActuales * puntosPorVidaRestante;
            float segundosAhorrados = Mathf.Max(0, tiempoParSegundos - tiempoPartida);
            bonusTiempo = Mathf.RoundToInt(segundosAhorrados * puntosPorSegundoAhorrado);
            puntuacionActual += bonusTiempo + bonusVidas;

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
            if(NotificacionFlotanteVR.Instancia != null)
            {
                NotificacionFlotanteVR.Instancia.MostrarNotificacion($"Si sientes que el juego no está calibrado, se recomienda mantener pulsado el botón Meta unos segundos y calibrar en ajustes", 5f);
            }
        }
        // Eliminado porque veo innecesario esto ahora
        //textoEstadisticas.text = $"TIEMPO: {tiempoFormat}\nBONUS TIEMPO: +{bonusTiempo}\nBONUS VIDAS: +{bonusVidas}\n\nPUNTUACIÓN FINAL: {puntuacionActual}";

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
                golpesD,
                puntuacionActual,
                vidasActuales
            );
        }
        if(textoPuntuacion != null)
        {
            textoPuntuacion.text = puntuacionActual.ToString("N0");
        }
        controladorMenu.MostrarResultadosFinales(mensaje);
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
            if (!enCuentaAtras) textoMensajes.text = "Partida pausada";
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

        if (pelotasActuales.Length > 40)
        {
            return; // Evitamos crear demasiadas pelotas que puedan colapsar el juego
        }

        foreach (GameObject pelota in pelotasActuales)
        {
            GameObject nuevaPelota = Instantiate(prefabPelota, pelota.transform.position, Quaternion.identity, transform);
            ComportamientoPelota scriptNueva = nuevaPelota.GetComponent<ComportamientoPelota>();

            Rigidbody rbOriginal = pelota.GetComponent<Rigidbody>();
            if (rbOriginal != null && scriptNueva != null)
            {
                Vector3 velNueva = new Vector3(-rbOriginal.linearVelocity.x, rbOriginal.linearVelocity.y, 0f);
                scriptNueva.Lanzar(velNueva);
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

    private void ConfigurarVidasIniciales()
    {
        if (contenedorCorazones != null && posicionEsquinaCorazones == Vector3.zero)
        {
            posicionEsquinaCorazones = contenedorCorazones.localPosition;
            escalaOriginalCorazones = contenedorCorazones.localScale;
        }

        // Leer dificultad y asignar vidas
        if (MonitorClinico.Instancia != null)
        {
            if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Facil) vidasActuales = vidasFacil;
            else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Normal) vidasActuales = vidasNormal;
            else vidasActuales = vidasDificil;
        }

        for (int i = 0; i < imagenesCorazones.Length; i++)
        {
            imagenesCorazones[i].gameObject.SetActive(i < vidasActuales);
            imagenesCorazones[i].sprite = spriteVidaLlena;
        }
    }

    public void SumarPuntos(int puntosBloque)
    {
        float multiplicador = 1.0f;
        if (MonitorClinico.Instancia != null)
        {
            if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Normal) multiplicador = 1.5f;
            else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Dificil) multiplicador = 2.0f;
        }

        puntuacionActual += Mathf.RoundToInt(puntosBloque * multiplicador);
        ActualizarTextoPuntuacion();
    }

    private IEnumerator RutinaPerderVida()
    {
        enCuentaAtras = true;
        float duracionAnim = 1f;
        float tiempo = 0f;

        // Hacemos zoom y movimiento hacia el centro del contenedor de corazones
        if (contenedorCorazones != null)
        {
            while (tiempo < duracionAnim)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionAnim);
                contenedorCorazones.localPosition = Vector3.Lerp(posicionEsquinaCorazones, Vector3.zero, t);
                contenedorCorazones.localScale = Vector3.Lerp(escalaOriginalCorazones, escalaOriginalCorazones * 2.5f, t);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);

        vidasActuales--;
        if (vidasActuales >= 0 && vidasActuales < imagenesCorazones.Length)
        {
            imagenesCorazones[vidasActuales].sprite = spriteVidaVacia;
            ReproducirSonidoGlobal(sonidoDerrota);
        }

        yield return new WaitForSeconds(1.5f);

        tiempo = 0f;
        if (contenedorCorazones != null)
        {
            while (tiempo < duracionAnim)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionAnim);
                contenedorCorazones.localPosition = Vector3.Lerp(Vector3.zero, posicionEsquinaCorazones, t);
                contenedorCorazones.localScale = Vector3.Lerp(escalaOriginalCorazones * 2.5f, escalaOriginalCorazones, t);
                yield return null;
            }
        }

        if (vidasActuales <= 0)
        {
            TerminarPartida("¡Has perdido!");
        }
        else
        {
            StartCoroutine(RutinaInicioPartida());
        }
    }

    public void ActualizarCorazonesUI()
    {
        int vidasConfig = 0;
        if (MonitorClinico.Instancia != null)
        {
            var dif = MonitorClinico.Instancia.dificultadActual;
            if (dif == MonitorClinico.NivelDificultad.Facil) vidasConfig = vidasFacil;
            else if (dif == MonitorClinico.NivelDificultad.Normal) vidasConfig = vidasNormal;
            else vidasConfig = vidasDificil;
        }

        for (int i = 0; i < imagenesCorazones.Length; i++)
        {
            imagenesCorazones[i].gameObject.SetActive(i < vidasConfig);
            imagenesCorazones[i].sprite = spriteVidaLlena;
        }
    }

    private void ActualizarTextoPuntuacion()
    {
        if (textoPuntuacion == null) return;

        string colorPuntos = "white";

        if (puntuacionActual > recordDelNivel && recordDelNivel > 0 && !recordSuperado)
        {
            colorPuntos = "yellow";
            recordSuperado = true;

            if (puntoAparicion != null && perfilTextoRecord != null)
            {
                Vector3 posTexto = puntoAparicion.position + new Vector3(0, 1.5f, 0);
                DynamicTextManager.CreateText(posTexto, "¡NUEVO RÉCORD!", perfilTextoRecord);
            }
        }

        textoPuntuacion.text = $"PUNTOS: <color={colorPuntos}>{puntuacionActual:N0}</color>\n<size=80%>RÉCORD: {recordDelNivel:N0}</size>";
    }

    public int ObtenerRecordPrevio()
    {
        return recordDelNivel;
    }
}