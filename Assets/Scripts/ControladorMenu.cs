using UnityEngine;
using TMPro;

public class ControladorMenuVR : MonoBehaviour
{
    [Header("Configuración del Menú")]
    public GameObject panelMenu;
    public Transform headAnchor;
    public PunteroLaserVR scriptLaser;

    [Header("Referencias UI")]
    public TextMeshProUGUI textoNumNivel;
    public TextMeshProUGUI textoStatsClinicas;

    public GameObject botonIzq, botonDer, botonJugar, botonReiniciar, botonVolverMenu;

    [Header("Ajustes Clínicos")]
    public float distanciaDeLaCara = 1.8f;

    private int nivelSeleccionado = 0;

    void Start()
    {
        // Al empezar, mostramos el menú principal
        panelMenu.SetActive(true);
        ActualizarVistaMenu();
        ColocarMenuDelanteDeLaMirada();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) ||
            OVRInput.GetDown(OVRInput.Button.Three) ||
            OVRInput.GetDown(OVRInput.Button.Start))
        {
            if (panelMenu == null || headAnchor == null) return;

            bool estaActivado = !panelMenu.activeSelf;
            panelMenu.SetActive(estaActivado);

            if (scriptLaser != null) scriptLaser.enabled = estaActivado;

            if (GestorArkanoid.Instancia != null)
            {
                GestorArkanoid.Instancia.AlternarPausa(estaActivado);
            }

            if (estaActivado)
            {
                ActualizarVistaMenu();
                ColocarMenuDelanteDeLaMirada();
            }
        }
    }

    void ActualizarVistaMenu()
    {
        // Leemos el alcance máximo de la pala y lo mostramos siempre en el menú
        if (GestorArkanoid.Instancia != null && GestorArkanoid.Instancia.controladorPala != null)
        {
            string romIzq = Mathf.Abs(GestorArkanoid.Instancia.controladorPala.maxEstiramientoIzquierda).ToString("F2");
            string romDer = Mathf.Abs(GestorArkanoid.Instancia.controladorPala.maxEstiramientoDerecha).ToString("F2");
            textoStatsClinicas.text = $"RANGO DE MOVIMIENTO ACTIVO\nIzquierda: {romIzq} | Derecha: {romDer}";
        }

        // Si el juego NO ha empezado, estamos en el Selector de Niveles
        if (GestorArkanoid.Instancia != null && !GestorArkanoid.Instancia.juegoEmpezado)
        {
            botonIzq.SetActive(true);
            botonDer.SetActive(true);
            botonJugar.SetActive(true);
            textoNumNivel.gameObject.SetActive(true);

            botonReiniciar.SetActive(false);
            botonVolverMenu.SetActive(false);

            textoNumNivel.text = "NIVEL " + (nivelSeleccionado + 1);
        }
        else // Si el juego HA empezado, estamos en la Pantalla de Pausa
        {
            botonIzq.SetActive(false);
            botonDer.SetActive(false);
            botonJugar.SetActive(false);
            textoNumNivel.gameObject.SetActive(false);

            botonReiniciar.SetActive(true);
            botonVolverMenu.SetActive(true);
        }
    }

    public void CambiarNivel(int direccion)
    {
        if (GestorArkanoid.Instancia == null) return;

        int totalNiveles = GestorArkanoid.Instancia.listaDeNiveles.Length;
        nivelSeleccionado += direccion;

        // Efecto carrusel (si pasas del último, vuelve al primero)
        if (nivelSeleccionado < 0) nivelSeleccionado = totalNiveles - 1;
        if (nivelSeleccionado >= totalNiveles) nivelSeleccionado = 0;

        textoNumNivel.text = "NIVEL " + (nivelSeleccionado + 1);
        GestorArkanoid.Instancia.CargarPrevisualizacion(nivelSeleccionado);
    }

    public void BotonUI_Jugar()
    {
        GestorArkanoid.Instancia.EmpezarPartidaDesdeMenu();
        CerrarMenu();
    }

    public void BotonUI_Reiniciar()
    {
        GestorArkanoid.Instancia.ReiniciarNivelActual();
        CerrarMenu();
    }

    public void BotonUI_VolverAlMenu()
    {
        GestorArkanoid.Instancia.VolverAlMenuPrincipal();
        ActualizarVistaMenu(); // Refresca los botones para mostrar el selector
    }

    private void CerrarMenu()
    {
        panelMenu.SetActive(false);
        if (scriptLaser != null) scriptLaser.enabled = false;
    }

    void ColocarMenuDelanteDeLaMirada()
    {
        UnityEngine.Vector3 headPos = headAnchor.position;
        UnityEngine.Vector3 lookDirection = headAnchor.forward;

        UnityEngine.Vector3 targetPos = headPos + (lookDirection.normalized * distanciaDeLaCara);
        targetPos.y = Mathf.Max(targetPos.y, headPos.y - 0.2f);

        transform.position = targetPos;
        transform.LookAt(headPos);
        transform.Rotate(0, 180, 0);
    }
}