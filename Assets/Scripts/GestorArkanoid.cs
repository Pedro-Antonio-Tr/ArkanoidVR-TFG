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

    [Header("Conexión Clínica")]
    public ControladorPalaVR controladorPala;

    [Header("Aparición de Pelota")]
    public GameObject prefabPelota;
    public Transform puntoAparicion;

    [Header("Estado de la Partida")]
    public int pelotasEnJuego = 0;
    public int bloquesRestantes = 0;
    public bool juegoEmpezado = false;

    // VARIABLES DE TIEMPO
    private float tiempoPartida = 0f;
    private bool cronometroActivo = false;
    private Coroutine rutinaReanudacion;

    void Awake() { Instancia = this; }

    void Start()
    {
        textoMensajes.text = "ABRE EL MENÚ PARA EMPEZAR";
        textoEstadisticas.text = "";
        CargarPrevisualizacion(0); // Cargamos el Nivel 1 de fondo
    }

    void Update()
    {
        if (cronometroActivo) tiempoPartida += Time.deltaTime;
    }

    public void CargarPrevisualizacion(int indice)
    {
        if (nivelActualInstanciado != null) Destroy(nivelActualInstanciado);

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
        CargarPrevisualizacion(nivelElegido); // Regenera los bloques
        EmpezarPartidaDesdeMenu();
    }

    public void VolverAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        juegoEmpezado = false;
        cronometroActivo = false;
        LimpiarPelotas();
        textoMensajes.text = "ABRE EL MENÚ PARA EMPEZAR";
        textoEstadisticas.text = "";
        CargarPrevisualizacion(nivelElegido);
    }

    IEnumerator RutinaInicioPartida()
    {
        juegoEmpezado = true;
        cronometroActivo = false;

        yield return new WaitForEndOfFrame();
        bloquesRestantes = GameObject.FindGameObjectsWithTag("Bloque").Length;

        textoMensajes.text = "3";
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "2";
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "1";
        yield return new WaitForSeconds(1f);
        textoMensajes.text = "¡GO!";

        Instantiate(prefabPelota, puntoAparicion.position, Quaternion.identity, transform);
        cronometroActivo = true; // Empieza a contar el tiempo

        yield return new WaitForSeconds(1f);
        if (textoMensajes.text == "¡GO!") textoMensajes.text = "";
    }

    public void RegistrarPelota() { pelotasEnJuego++; }

    public void PelotaDestruida()
    {
        pelotasEnJuego--;
        if (pelotasEnJuego <= 0 && bloquesRestantes > 0) TerminarPartida("¡FIN DEL JUEGO!");
    }

    public void BloqueDestruido()
    {
        bloquesRestantes--;
        if (bloquesRestantes <= 0)
        {
            LimpiarPelotas();
            TerminarPartida("¡NIVEL COMPLETADO!");
        }
    }

    void TerminarPartida(string mensaje)
    {
        cronometroActivo = false;
        juegoEmpezado = false;
        textoMensajes.text = mensaje;

        // Formatear tiempo en Minutos:Segundos
        int minutos = Mathf.FloorToInt(tiempoPartida / 60F);
        int segundos = Mathf.FloorToInt(tiempoPartida - minutos * 60);
        textoEstadisticas.text = $"TIEMPO: {string.Format("{0:00}:{1:00}", minutos, segundos)}";
    }

    void LimpiarPelotas()
    {
        GameObject[] pelotas = GameObject.FindGameObjectsWithTag("Pelota");
        foreach (GameObject p in pelotas) Destroy(p);
        pelotasEnJuego = 0;
    }

    public void AlternarPausa(bool estaEnPausa)
    {
        if (!juegoEmpezado) return;

        if (rutinaReanudacion != null) StopCoroutine(rutinaReanudacion);

        if (estaEnPausa)
        {
            Time.timeScale = 0f;
            cronometroActivo = false;
            textoMensajes.text = "PAUSA";
        }
        else
        {
            rutinaReanudacion = StartCoroutine(RutinaDescongelarTiempo());
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
}