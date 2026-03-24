using UnityEngine;
using TMPro;

public class ControladorMenuVR : MonoBehaviour
{
    [Header("Configuración Base")]
    public GameObject panelMenu; // El Fondo_Menu
    public Transform headAnchor;
    public PunteroLaserVR scriptLaser;

    [Header("Máquina de Estados (Paneles)")]
    public GameObject panelBienvenida;
    public GameObject panelNiveles;
    public GameObject panelPausa;
    public GameObject panelAjustes;

    [Header("Centrado de Vista Clínico")]
    public Transform pantallaArkanoid;
    public float distanciaPantallaArkanoid = 12f; // A cuántos metros se coloca la pantalla grande
    public float distanciaMenu = 1.8f; // A cuántos metros sale el menú emergente

    [Header("Referencias UI Antiguas (Niveles)")]
    public TextMeshProUGUI textoNumNivel;
    public TextMeshProUGUI textoStatsClinicas;
    private int nivelSeleccionado = 0;

    private bool primeraVezAbierto = true;

    void Start()
    {
        // Al arrancar, mostramos el menú de bienvenida
        panelMenu.SetActive(true);
        AbrirPanel(panelBienvenida);
        ColocarMenuDelanteDeLaMirada();
    }

    void Update()
    {
        // Botones de las gafas (A, X, o Menú)
        if (OVRInput.GetDown(OVRInput.Button.One) ||
            OVRInput.GetDown(OVRInput.Button.Three) ||
            OVRInput.GetDown(OVRInput.Button.Start))
        {
            AlternarMenuGeneral();
        }
    }

    public void AlternarMenuGeneral()
    {
        if (panelMenu == null || headAnchor == null) return;

        bool estaActivado = !panelMenu.activeSelf;
        panelMenu.SetActive(estaActivado);

        if (scriptLaser != null) scriptLaser.enabled = estaActivado;

        // Pausamos o reanudamos el juego
        if (GestorArkanoid.Instancia != null)
        {
            GestorArkanoid.Instancia.AlternarPausa(estaActivado);
        }

        if (estaActivado)
        {
            ColocarMenuDelanteDeLaMirada();

            // Decidimos qué panel mostrar al abrir el menú
            if (primeraVezAbierto)
            {
                AbrirPanel(panelBienvenida);
            }
            else if (GestorArkanoid.Instancia != null && !GestorArkanoid.Instancia.juegoEmpezado)
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

    // Apaga todos los paneles y enciende solo el que le pasemos
    public void AbrirPanel(GameObject panelDestino)
    {
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
        Vector3 lookDirection = headAnchor.forward; // El vector natural de la mirada (incluso tumbado)

        Vector3 posPantalla = headPos + (lookDirection.normalized * distanciaPantallaArkanoid);
        pantallaArkanoid.position = posPantalla;

        // Hacemos que la pantalla nos mire, y le damos la vuelta para que el Quad no se vea invertido
        pantallaArkanoid.LookAt(headPos);
        pantallaArkanoid.Rotate(0, 180, 0);

        ColocarMenuDelanteDeLaMirada();
    }

    void ColocarMenuDelanteDeLaMirada()
    {
        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

        Vector3 targetPos = headPos + (lookDirection.normalized * distanciaMenu);
        targetPos.y = Mathf.Max(targetPos.y, headPos.y - 0.2f);

        transform.position = targetPos;
        transform.LookAt(headPos);
        transform.Rotate(0, 180, 0);
    }

    // Botón "Empezar" del panel de Bienvenida
    public void BotonUI_AvanzarDesdeBienvenida()
    {
        primeraVezAbierto = false;
        AbrirPanel(panelNiveles);
        CambiarNivel(0);
    }

    // Botones del panel de Niveles
    public void CambiarNivel(int direccion)
    {
        if (GestorArkanoid.Instancia == null) return;

        int totalNiveles = GestorArkanoid.Instancia.listaDeNiveles.Length;
        nivelSeleccionado += direccion;

        if (nivelSeleccionado < 0) nivelSeleccionado = totalNiveles - 1;
        if (nivelSeleccionado >= totalNiveles) nivelSeleccionado = 0;

        textoNumNivel.text = "NIVEL " + (nivelSeleccionado + 1);
        GestorArkanoid.Instancia.CargarPrevisualizacion(nivelSeleccionado);
    }

    public void BotonUI_Jugar()
    {
        GestorArkanoid.Instancia.EmpezarPartidaDesdeMenu();
        AlternarMenuGeneral(); // Cierra el menú
    }

    // Botones del panel de Pausa
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

    // Botones de navegación hacia Ajustes
    public void BotonUI_IrAAjustes() { 
        AbrirPanel(panelAjustes); 
    }
    public void BotonUI_VolverAAjustesAnterior()
    {
        if (!GestorArkanoid.Instancia.juegoEmpezado) AbrirPanel(panelNiveles);
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
}