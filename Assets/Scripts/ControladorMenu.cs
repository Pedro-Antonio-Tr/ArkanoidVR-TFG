using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControladorMenu : MonoBehaviour
{
    public static ControladorMenu Instancia;

    [Header("Configuración Base")]
    public GameObject panelMenu;
    public Transform headAnchor;

    [Header("Punteros Láser")]
    public PunteroLaserVR laserIzquierdo;
    public PunteroLaserVR laserDerecho;
    public UnityEngine.EventSystems.OVRInputModule inputModule;

    [Header("Paneles")]
    public GameObject panelBienvenida;
    public GameObject panelNiveles;
    public GameObject panelPausa;
    public GameObject panelAjustes;
    public GameObject panelResultados;

    [Header("Botones de Modo (Para oscurecer en todos los menús)")]
    public Button[] botonesMandoIzq;
    public Button[] botonesMandoDer;
    public Button[] botonesMandoAmbos;

    [Header("Botones de Dificultad")]
    public Button[] botonesDifFacil;
    public Button[] botonesDifNormal;
    public Button[] botonesDifDificil;

    [Header("Centrado de Vista")]
    public Transform pantallaArkanoid;
    public float distanciaPantallaArkanoid = 3.5f;
    public float distanciaMenu = 1.8f;

    [Header("Referencias UI Antiguas (Niveles)")]
    public TextMeshProUGUI textoNumNivel;
    public TextMeshProUGUI textoStatsClinicas;
    private int nivelSeleccionado = 0;

    [Header("Calibración de Palas")]
    public GameObject panelCuentaAtras;
    public TextMeshProUGUI textoCuentaAtras;
    public TextMeshProUGUI textoInstrucciones;
    public ControladorPalaVR controladorPala;

    [Header("Ajustes de Sonido")] // En un futuro a lo mejor añado para música también
    public Slider sliderVolumen;

    [Header("Sonidos de Interfaz")]
    public AudioClip sonidoBoton;
    private AudioSource audioSourceMenu;

    [Header("Pantalla")]
    public Toggle togglePantallaCurva;

    [Header("Pantallas Físicas")]
    public GameObject pantallaPlana;
    public GameObject pantallaCurva;

    private bool primeraVezAbierto = true;
    private bool calibracionEnProceso = false;

    [Header("Sliders de Distancia")]
    public Slider sliderDistanciaMenu;
    public Slider sliderDistanciaPantalla;

    [Header("Textos Panel Resultados")]
    public TextMeshProUGUI textoTituloResultados;
    public TextMeshProUGUI textoStatsResultados;

    private bool partidaTerminada = false;

    void Start()
    {
        audioSourceMenu = gameObject.AddComponent<AudioSource>();
        audioSourceMenu.playOnAwake = false;
        audioSourceMenu.ignoreListenerPause = true;
        panelMenu.SetActive(true);
        AbrirPanel(panelBienvenida);

        if (GestorDatosUsuario.Instancia != null)
        {
            StartCoroutine(RutinaAplicarConfiguracionInicial());
        }
        else
        {
            ColocarMenuDelanteDeLaMirada();
            ActualizarLaseres(true);
            ActualizarBotonesModo();
            ActualizarBotonesDificultad();
        }

        if (GestorArkanoid.Instancia != null)
        {
            GestorArkanoid.Instancia.ActualizarCorazonesUI();
        }
    }

    private System.Collections.IEnumerator RutinaAplicarConfiguracionInicial()
    {
        yield return null;

        DatosConfiguracion config = GestorDatosUsuario.Instancia.configActual;

        if (sliderVolumen != null)
        {
            sliderVolumen.value = config.volumen;
        }
        AudioListener.volume = config.volumen;

        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)config.dificultad;
        }

        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.modoActual = (MonitorClinico.ModoControl)config.modoMando;
        }

        bool esCurva = config.pantallaCurva;
        if (togglePantallaCurva != null) togglePantallaCurva.isOn = esCurva;
        if (pantallaPlana != null) pantallaPlana.SetActive(!esCurva);
        if (pantallaCurva != null) pantallaCurva.SetActive(esCurva);

        distanciaMenu = config.distanciaMenu;
        if (sliderDistanciaMenu != null) sliderDistanciaMenu.value = config.distanciaMenu;

        float distInicial = esCurva ? config.distanciaCurva : config.distanciaPlana;
        distanciaPantallaArkanoid = distInicial;
        if (sliderDistanciaPantalla != null) sliderDistanciaPantalla.value = distInicial;

        if (esCurva && pantallaCurva != null)
        {
            float factorEscala = distInicial / 3.0f;
            pantallaCurva.transform.localScale = new Vector3(factorEscala, factorEscala, factorEscala);
        }

        CentrarVistaUsuario();

        if (pantallaArkanoid != null && config.inclinacionPantallaX != 0f)
        {
            Vector3 rotacionActual = pantallaArkanoid.eulerAngles;
            pantallaArkanoid.eulerAngles = new Vector3(config.inclinacionPantallaX, rotacionActual.y, rotacionActual.z);
        }

        ActualizarLaseres(true);
        ActualizarBotonesModo();
        ActualizarBotonesDificultad();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) ||
            OVRInput.GetDown(OVRInput.Button.Three) ||
            OVRInput.GetDown(OVRInput.Button.Start) || 
            partidaTerminada)
        {
            partidaTerminada = false;
            AlternarMenuGeneral();
        }
    }

    public void ReproducirSonidoClic()
    {
        if (sonidoBoton != null && audioSourceMenu != null)
        {
            audioSourceMenu.PlayOneShot(sonidoBoton);
        }
    }

    public void AlternarMenuGeneral()
    {
        //Añado que si el menú está abierto y no hay partida empezada ni en cuenta atrás, no se pueda cerrar el menú.
        if (panelMenu == null || headAnchor == null || calibracionEnProceso || GestorArkanoid.Instancia == null || (panelMenu.activeSelf && !GestorArkanoid.Instancia.juegoEmpezado && !GestorArkanoid.Instancia.enCuentaAtras)) return;

        bool estaActivado = !panelMenu.activeSelf;
        panelMenu.SetActive(estaActivado);

        ActualizarLaseres(estaActivado);

        if (GestorArkanoid.Instancia != null)
        {
            GestorArkanoid.Instancia.AlternarPausa(estaActivado);
        }

        if (estaActivado)
        {
            ColocarMenuDelanteDeLaMirada();
            if (partidaTerminada)
            {
                AbrirPanel(panelResultados);
                partidaTerminada = false;
            }
            else if (primeraVezAbierto)
            {
                AbrirPanel(panelBienvenida);
            }
            else if (GestorArkanoid.Instancia != null && !GestorArkanoid.Instancia.juegoEmpezado && !GestorArkanoid.Instancia.enCuentaAtras)
            {
                AbrirPanel(panelNiveles);
            }
            else
            {
                ActualizarStatsPausa();
                AbrirPanel(panelPausa);
            }
        }
    }

    public void AbrirPanel(GameObject panelDestino)
    {
        panelCuentaAtras.SetActive(false);
        panelBienvenida.SetActive(false);
        panelNiveles.SetActive(false);
        panelPausa.SetActive(false);
        panelAjustes.SetActive(false);
        panelResultados.SetActive(false);

        panelDestino.SetActive(true);
    }

    public void CentrarVistaUsuario()
    {
        if (headAnchor == null || pantallaArkanoid == null) return;

        Vector3 headPos = headAnchor.position;
        if (headPos.y < 0.5f)
        {
            headPos.y = 1.8f;
            Debug.LogWarning("Tracking no listo al centrar. Usando altura de seguridad 1.5m");
        }

        Vector3 lookDirection = headAnchor.forward;
        if (Mathf.Abs(lookDirection.y) > 0.8f)
        {
            lookDirection = Vector3.ProjectOnPlane(lookDirection, Vector3.up);
        }

        Vector3 posPantalla = headPos + (lookDirection.normalized * distanciaPantallaArkanoid);
        pantallaArkanoid.position = posPantalla;

        pantallaArkanoid.LookAt(headPos);
        pantallaArkanoid.Rotate(0, 180, 0);

        ColocarMenuDelanteDeLaMirada();

        if (GestorDatosUsuario.Instancia != null)
        {
            // Guardamos la rotación X (inclinación hacia arriba/abajo) de la pantalla
            GestorDatosUsuario.Instancia.configActual.inclinacionPantallaX = pantallaArkanoid.eulerAngles.x;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    void ColocarMenuDelanteDeLaMirada()
    {
        if (headAnchor == null) return;

        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

        if (headPos.y < 0.5f)
        {
            headPos.y = 1.5f;
            if (lookDirection == Vector3.zero) lookDirection = Vector3.forward;
        }

        Vector3 targetPos = headPos + (lookDirection.normalized * distanciaMenu);
        targetPos.y = Mathf.Max(targetPos.y, headPos.y - 0.2f);

        transform.position = targetPos;
        transform.LookAt(headPos);
        transform.Rotate(0, 180, 0);
    }

    public void BotonUI_CambiarMandoActivo(int modoElegido)
    {
        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.modoActual = (MonitorClinico.ModoControl)modoElegido;
        }
        ActualizarLaseres(true);
        ActualizarBotonesModo();

        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.modoMando = modoElegido;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    private void ActualizarBotonesModo()
    {
        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null)
        {
            modo = MonitorClinico.Instancia.modoActual;
        }

        foreach (Button btn in botonesMandoIzq)
        {
            if (btn != null)
            {
                btn.interactable = (modo != MonitorClinico.ModoControl.Izquierdo);
            }
        }

        foreach (Button btn in botonesMandoDer)
        {
            if (btn != null)
            {
                btn.interactable = (modo != MonitorClinico.ModoControl.Derecho);
            }
        }

        foreach (Button btn in botonesMandoAmbos)
        {
            if (btn != null)
            {
                btn.interactable = (modo != MonitorClinico.ModoControl.Ambos);
            }
        }
    }

    private void ActualizarLaseres(bool menuAbierto)
    {
        if (inputModule != null)
        {
            inputModule.enabled = menuAbierto;
        }

        if (!menuAbierto)
        {
            if (laserIzquierdo != null)
            {
                laserIzquierdo.enabled = false;
            }
            if (laserDerecho != null)
            {
                laserDerecho.enabled = false;
            }
            return;
        }

        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null)
        {
            modo = MonitorClinico.Instancia.modoActual;
        }

        if (modo == MonitorClinico.ModoControl.Izquierdo)
        {
            if (laserIzquierdo != null)
            {
                laserIzquierdo.enabled = true;
            }
            if (laserDerecho != null)
            {
                laserDerecho.enabled = false;
            }
            if (inputModule != null)
            {
                inputModule.rayTransform = laserIzquierdo.transform;
            }
        }
        else if (modo == MonitorClinico.ModoControl.Derecho)
        {
            if (laserIzquierdo != null)
            {
                laserIzquierdo.enabled = false;
            }
            if (laserDerecho != null)
            {
                laserDerecho.enabled = true;
            }
            if (inputModule != null)
            {
                inputModule.rayTransform = laserDerecho.transform;
            }
        }
        else
        {
            if (laserIzquierdo != null)
            {
                laserIzquierdo.enabled = true;
            }
            if (laserDerecho != null)
            {
                laserDerecho.enabled = true;
            }
            if (inputModule != null)
            {
                inputModule.rayTransform = laserDerecho.transform;
            }
        }
    }

    public void BotonUI_AvanzarDesdeBienvenida()
    {
        primeraVezAbierto = false;
        AbrirPanel(panelNiveles);
        CambiarNivel(0);
    }

    public void CambiarNivel(int direccion)
    {
        if (GestorArkanoid.Instancia == null) return;

        int totalNiveles = GestorArkanoid.Instancia.listaDeNiveles.Length;
        nivelSeleccionado += direccion;

        if (nivelSeleccionado < 0)
        {
            nivelSeleccionado = totalNiveles - 1;
        }
        if (nivelSeleccionado >= totalNiveles)
        {
            nivelSeleccionado = 0;
        }

        textoNumNivel.text = "NIVEL " + (nivelSeleccionado + 1);
        GestorArkanoid.Instancia.CargarPrevisualizacion(nivelSeleccionado);
    }

    public void BotonUI_Jugar()
    {
        GestorArkanoid.Instancia.EmpezarPartidaDesdeMenu();
        AlternarMenuGeneral();
    }

    public void BotonUI_Reiniciar()
    {
        GestorArkanoid.Instancia.ReiniciarNivelActual();
        AlternarMenuGeneral();
    }

    public void BotonUI_VolverAlMenu()
    {
        GestorArkanoid.Instancia.VolverAlMenuPrincipal();
        AbrirPanel(panelNiveles);
    }

    public void BotonUI_IrAAjustes()
    {
        AbrirPanel(panelAjustes);
    }

    public void BotonUI_VolverAAjustesAnterior()
    {
        if (partidaTerminada)
        {
            AbrirPanel(panelResultados);
        }
        else if (primeraVezAbierto)
        {
            AbrirPanel(panelBienvenida);
        }
        else if (GestorArkanoid.Instancia != null && !GestorArkanoid.Instancia.juegoEmpezado && !GestorArkanoid.Instancia.enCuentaAtras)
        {
            AbrirPanel(panelNiveles);
        }
        else AbrirPanel(panelPausa);
    }

    void ActualizarStatsPausa()
    {
        if (GestorArkanoid.Instancia != null && MonitorClinico.Instancia != null)
        {
            string nivelStr = "NIVEL " + (GestorArkanoid.Instancia.nivelElegido + 1);
            string difStr = MonitorClinico.Instancia.dificultadActual.ToString().ToUpper();
            string tiempoStr = GestorArkanoid.Instancia.ObtenerTiempoFormateado();
            int bloques = GestorArkanoid.Instancia.bloquesRestantes;
            int vidaTotal = GestorArkanoid.Instancia.ObtenerVidaTotalBloques();

            if (textoStatsClinicas != null)
            {
                textoStatsClinicas.text =
                    $"<color=#FFD700>{nivelStr}</color> | DIFICULTAD: {difStr}\n\n" +
                    $"TIEMPO DE JUEGO: {tiempoStr}\n" +
                    $"BLOQUES RESTANTES: {bloques}\n" +
                    $"VIDA TOTAL RESTANTE: {vidaTotal}";
            }
        }
    }

    public void CambiarVolumenGeneral()
    {
        if (sliderVolumen != null)
        {
            AudioListener.volume = sliderVolumen.value;
        }

        if (GestorDatosUsuario.Instancia != null && sliderVolumen != null)
        {
            GestorDatosUsuario.Instancia.configActual.volumen = sliderVolumen.value;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    public void BotonUI_CambiarDificultad(int difElegida)
    {
        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)difElegida;
        }
        ActualizarBotonesDificultad();

        if (GestorArkanoid.Instancia != null)
        {
            GestorArkanoid.Instancia.ActualizarCorazonesUI();
        }

        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.dificultad = difElegida;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    private void ActualizarBotonesDificultad()
    {
        MonitorClinico.NivelDificultad dif = MonitorClinico.NivelDificultad.Normal;
        if (MonitorClinico.Instancia != null)
        {
            dif = MonitorClinico.Instancia.dificultadActual;
        }

        foreach (var btn in botonesDifFacil)
        {
            if (btn != null)
            {
                btn.interactable = (dif != MonitorClinico.NivelDificultad.Facil);
            }
        }
        foreach (var btn in botonesDifNormal)
        {
            if (btn != null)
            {
                btn.interactable = (dif != MonitorClinico.NivelDificultad.Normal);
            }
        }
        foreach (var btn in botonesDifDificil)
        {
            if (btn != null)
            {
                btn.interactable = (dif != MonitorClinico.NivelDificultad.Dificil);
            }
        }
    }

    public void BotonUI_IniciarCalibracion()
    {
        StartCoroutine(RutinaCalibrarCentro());
    }

    private System.Collections.IEnumerator RutinaCalibrarCentro()
    {
        calibracionEnProceso = true;
        panelAjustes.SetActive(false);
        panelCuentaAtras.SetActive(true);

        MonitorClinico.ModoControl modo = MonitorClinico.Instancia.modoActual;

        // Si el modo es "Ambos", calibramos primero un brazo y luego el otro
        if (modo == MonitorClinico.ModoControl.Ambos)
        {
            yield return CalibrarBrazoCompleto(OVRInput.Controller.LTouch, "BRAZO IZQUIERDO");

            yield return CalibrarBrazoCompleto(OVRInput.Controller.RTouch, "BRAZO DERECHO");
        }
        else
        {
            OVRInput.Controller mando = (modo == MonitorClinico.ModoControl.Izquierdo) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            yield return CalibrarBrazoCompleto(mando, "CALIBRACIÓN");
        }

        GestorDatosUsuario.Instancia.GuardarConfiguracion();
        textoInstrucciones.text = "¡CALIBRACIÓN COMPLETA!";
        textoCuentaAtras.text = "";
        yield return new WaitForSecondsRealtime(2f);

        panelCuentaAtras.SetActive(false);
        panelAjustes.SetActive(true);
        calibracionEnProceso = false;
    }

    private System.Collections.IEnumerator CalibrarBrazoCompleto(OVRInput.Controller mando, string nombreBrazo)
    {
        yield return FaseContador(nombreBrazo + ": PON EL MANDO EN EL CENTRO");
        float centro = OVRInput.GetLocalControllerPosition(mando).x;

        yield return FaseContador(nombreBrazo + ": ESTIRA A LA IZQUIERDA");
        float izq = OVRInput.GetLocalControllerPosition(mando).x;

        yield return FaseContador(nombreBrazo + ": ESTIRA A LA DERECHA");
        float der = OVRInput.GetLocalControllerPosition(mando).x;

        if (mando == OVRInput.Controller.LTouch)
        {
            GestorDatosUsuario.Instancia.configActual.centroX_L = centro;
            GestorDatosUsuario.Instancia.configActual.alcanceIzqX_L = izq;
            GestorDatosUsuario.Instancia.configActual.alcanceDerX_L = der;
        }
        else
        {
            GestorDatosUsuario.Instancia.configActual.centroX_R = centro;
            GestorDatosUsuario.Instancia.configActual.alcanceIzqX_R = izq;
            GestorDatosUsuario.Instancia.configActual.alcanceDerX_R = der;
        }
    }

    private System.Collections.IEnumerator FaseContador(string instruccion)
    {
        textoInstrucciones.text = instruccion;
        for (int i = 5; i > 0; i--)
        {
            textoCuentaAtras.text = i.ToString();
            ReproducirSonidoClic();
            yield return new WaitForSecondsRealtime(1f);
        }
        textoCuentaAtras.text = "OK";
        yield return new WaitForSecondsRealtime(0.5f);
    }

    public void MostrarResultadosFinales(string titulo)
    {
        partidaTerminada = true;

        if (textoTituloResultados != null)
        {
            textoTituloResultados.text = titulo;
        }

        if (textoStatsResultados != null)
        {
            string tiempo = GestorArkanoid.Instancia.ObtenerTiempoFormateado();
            string nivel = "NIVEL " + (GestorArkanoid.Instancia.nivelElegido + 1);
            int bloques = GestorArkanoid.Instancia.bloquesRestantes;

            textoStatsResultados.text = $"{nivel}\nTIEMPO: {tiempo}\nBLOQUES RESTANTES: {bloques}\n PUNTUACIÓN OBTENIDA: {GestorArkanoid.Instancia.puntuacionActual}";
        }

        if (!panelMenu.activeSelf)
        {
            AlternarMenuGeneral();
        }
        else
        {
            AbrirPanel(panelResultados); 
        }
    }

    public void CambiarCurvaturaPantalla()
    {
        bool activarCurva = togglePantallaCurva.isOn;
        // Guardamos en el perfil del paciente
        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.pantallaCurva = activarCurva;

            // Sincronizamos el Slider con la distancia guardada para ese tipo de pantalla
            float distanciaGuardada = activarCurva ?
                GestorDatosUsuario.Instancia.configActual.distanciaCurva :
                GestorDatosUsuario.Instancia.configActual.distanciaPlana;

            sliderDistanciaPantalla.value = distanciaGuardada;

            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }

        // Aplicamos a la pantalla
        if (pantallaPlana != null) pantallaPlana.SetActive(!activarCurva);
        if (pantallaCurva != null) pantallaCurva.SetActive(activarCurva);

        CentrarVistaUsuario();
    }

    public void CambiarDistanciaMenu(float nuevaDist)
    {
        sliderDistanciaMenu.value = nuevaDist;

        distanciaMenu = nuevaDist;
        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.distanciaMenu = nuevaDist;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
        ColocarMenuDelanteDeLaMirada();
    }

    public void CambiarDistanciaPantalla(float nuevaDist)
    {
        distanciaPantallaArkanoid = nuevaDist;
        bool esCurva = togglePantallaCurva.isOn;

        if (GestorDatosUsuario.Instancia != null)
        {
            if (esCurva) GestorDatosUsuario.Instancia.configActual.distanciaCurva = nuevaDist;
            else GestorDatosUsuario.Instancia.configActual.distanciaPlana = nuevaDist;

            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }

        // Si la pantalla es curva, escalamos su tamaño para que siga envolviendo al jugador
        // Un radio mayor requiere una escala mayor para mantener los 120º
        if (esCurva && pantallaCurva != null)
        {
            // La escala base 1 es para distancia 3. Calculamos la proporción.
            float factorEscala = nuevaDist / 3.0f;
            pantallaCurva.transform.localScale = new Vector3(factorEscala, factorEscala, factorEscala);
        }

        CentrarVistaUsuario();
    }
}