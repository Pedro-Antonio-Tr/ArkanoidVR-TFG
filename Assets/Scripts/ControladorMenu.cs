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

        ActualizarLaseres(true);
        ActualizarBotonesModo();
        ActualizarBotonesDificultad();
        if (sliderVolumen != null)
        {
            sliderVolumen.value = AudioListener.volume;
        }
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

    public void BotonUI_IrAAjustes() { AbrirPanel(panelAjustes); }

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
        if (sliderVolumen != null) AudioListener.volume = sliderVolumen.value;
    }

    public void BotonUI_CambiarDificultad(int difElegida)
    {
        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)difElegida;
        }
        ActualizarBotonesDificultad();
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

        for (int i = 3; i > 0; i--)
        {
            if (textoCuentaAtras != null)
            {
                textoCuentaAtras.text = i.ToString();
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = "¡CALIBRADO!";
        }
        yield return new WaitForSecondsRealtime(0.5f);

        // Cálculo del punto intermedio según los mandos activos
        float nuevoCentroX = 0f;
        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null)
        {
            modo = MonitorClinico.Instancia.modoActual;
        }

        Vector3 posIzq = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Vector3 posDer = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        if (modo == MonitorClinico.ModoControl.Izquierdo)
        {
            nuevoCentroX = posIzq.x;
        }
        else if (modo == MonitorClinico.ModoControl.Derecho)
        {
            nuevoCentroX = posDer.x;
        }
        else
        {
            nuevoCentroX = (posIzq.x + posDer.x) / 2f; // Media de los dos si se está jugando con ambos mandos
        }

        if (controladorPala != null)
        {
            controladorPala.offsetXCentrado = nuevoCentroX;
        }

        if (panelCuentaAtras != null)
        {
            panelCuentaAtras.SetActive(false);
        }
        panelAjustes.SetActive(true);
    }
}