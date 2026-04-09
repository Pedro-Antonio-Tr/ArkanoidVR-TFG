using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControladorMenu : MonoBehaviour
{
    [Header("Configuración Base")]
    public GameObject panelMenu;
    public Transform headAnchor;

    [Header("Punteros Láser")]
    public PunteroLaserVR laserIzquierdo;
    public PunteroLaserVR laserDerecho;
    public UnityEngine.EventSystems.OVRInputModule inputModule;

    [Header("Máquina de Estados (Paneles)")]
    public GameObject panelBienvenida;
    public GameObject panelNiveles;
    public GameObject panelPausa;
    public GameObject panelAjustes;

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
    public ControladorPalaVR controladorPala;

    [Header("Ajustes de Sonido")] // En un futuro a lo mejor añado para música también
    public Slider sliderVolumen;

    [Header("Sonidos de Interfaz")]
    public AudioClip sonidoBoton;
    private AudioSource audioSourceMenu;

    private bool primeraVezAbierto = true;

    void Start()
    {
        audioSourceMenu = gameObject.AddComponent<AudioSource>();
        audioSourceMenu.playOnAwake = false;
        audioSourceMenu.ignoreListenerPause = true;
        panelMenu.SetActive(true);
        AbrirPanel(panelBienvenida);
        ColocarMenuDelanteDeLaMirada();

        if (sliderVolumen != null)
        {
            sliderVolumen.value = AudioListener.volume;
        }

        if (GestorDatosUsuario.Instancia != null)
        {
            DatosConfiguracion config = GestorDatosUsuario.Instancia.configActual;

            // Volumen
            if (sliderVolumen != null)
            {
                sliderVolumen.value = config.volumen;
            }
            AudioListener.volume = config.volumen;

            // Dificultad
            if (MonitorClinico.Instancia != null)
            {
                MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)config.dificultad;
            }

            // Modo de Mando
            if (MonitorClinico.Instancia != null)
            {
                MonitorClinico.Instancia.modoActual = (MonitorClinico.ModoControl)config.modoMando;
            }

            // Inclinación de Pantalla (Si ya tenía una guardada)
            if (pantallaArkanoid != null && config.inclinacionPantallaX != 0f)
            {
                // Solo rotamos el eje X (cabeceo) y dejamos los demás quietos
                Vector3 rotacionActual = pantallaArkanoid.eulerAngles;
                pantallaArkanoid.eulerAngles = new Vector3(config.inclinacionPantallaX, rotacionActual.y, rotacionActual.z);
            }
        }

        ActualizarLaseres(true);
        ActualizarBotonesModo();
        ActualizarBotonesDificultad();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) ||
            OVRInput.GetDown(OVRInput.Button.Three) ||
            OVRInput.GetDown(OVRInput.Button.Start))
        {
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
        if (panelMenu == null || headAnchor == null) return;

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

            if (primeraVezAbierto)
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

        panelDestino.SetActive(true);
    }

    public void CentrarVistaUsuario()
    {
        if (headAnchor == null || pantallaArkanoid == null) return;

        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

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
        if (primeraVezAbierto)
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
        if (GestorArkanoid.Instancia != null && GestorArkanoid.Instancia.controladorPala != null)
        {
            string romIzq = Mathf.Abs(GestorArkanoid.Instancia.controladorPala.maxEstiramientoIzquierda).ToString("F2");
            string romDer = Mathf.Abs(GestorArkanoid.Instancia.controladorPala.maxEstiramientoDerecha).ToString("F2");
            textoStatsClinicas.text = $"ROM ACTIVO\nIzq: {romIzq} | Der: {romDer}";
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
        panelAjustes.SetActive(false);
        if (panelCuentaAtras != null)
        {
            panelCuentaAtras.SetActive(true);
        }

        // FASE 1: CENTRO
        yield return EjecutarFaseCalibracion("1/3: PON LAS MANOS EN TU CENTRO Y ESPERA...");
        float centro = ObtenerPosicionMandoActivoX();
        GestorDatosUsuario.Instancia.configActual.centroX = centro;

        // FASE 2: TOPE IZQUIERDO
        yield return EjecutarFaseCalibracion("2/3: ESTIRA AL MÁXIMO A TU IZQUIERDA...");
        float topeIzq = ObtenerPosicionMandoActivoX();
        // Si no estiró suficiente, le ponemos un mínimo de 10cm para que no se rompa la física
        if (topeIzq > centro - 0.1f)
        {
            topeIzq = centro - 0.1f;
        }
        GestorDatosUsuario.Instancia.configActual.alcanceIzqX = topeIzq;

        // FASE 3: TOPE DERECHO
        yield return EjecutarFaseCalibracion("3/3: ESTIRA AL MÁXIMO A TU DERECHA...");
        float topeDer = ObtenerPosicionMandoActivoX();
        if (topeDer < centro + 0.1f)
        {
            topeDer = centro + 0.1f;
        }
        GestorDatosUsuario.Instancia.configActual.alcanceDerX = topeDer;

        // GUARDAMOS EN EL JSON
        GestorDatosUsuario.Instancia.GuardarConfiguracion();

        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = "¡CALIBRACIÓN GUARDADA!";
        }
        ReproducirSonidoClic();
        yield return new WaitForSecondsRealtime(1.5f);

        if (panelCuentaAtras != null)
        {
            panelCuentaAtras.SetActive(false);
        }
        panelAjustes.SetActive(true);
    }

    private System.Collections.IEnumerator EjecutarFaseCalibracion(string mensaje)
    {
        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = mensaje + "\n\n3";
        }
        ReproducirSonidoClic();
        yield return new WaitForSecondsRealtime(1f);
        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = mensaje + "\n\n2";
        }
        ReproducirSonidoClic();
        yield return new WaitForSecondsRealtime(1f);
        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = mensaje + "\n\n1";
        }
        ReproducirSonidoClic();
        yield return new WaitForSecondsRealtime(1f);
    }

    private float ObtenerPosicionMandoActivoX()
    {
        MonitorClinico.ModoControl modo = MonitorClinico.Instancia.modoActual;
        float posIzq = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch).x;
        float posDer = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch).x;

        if (modo == MonitorClinico.ModoControl.Izquierdo)
        {
            return posIzq;
        }
        if (modo == MonitorClinico.ModoControl.Derecho)
        {
            return posDer;
        }
        return (posIzq + posDer) / 2f; // Si usa ambas, hacemos la media
    }
}