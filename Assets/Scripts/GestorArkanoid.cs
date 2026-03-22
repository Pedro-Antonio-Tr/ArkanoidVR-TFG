using UnityEngine;
using TMPro;
using System.Collections;

public class GestorArkanoid : MonoBehaviour
{
    public static GestorArkanoid Instancia;

    private Coroutine rutinaReanudacion;
    private bool juegoEmpezado = false;

    [Header("Selector de Niveles")]
    public GameObject[] listaDeNiveles;
    public int nivelElegido = 0;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI textoMensajes;
    public TextMeshProUGUI textoEstadisticas;

    [Header("Conexión Clínica")]
    public ControladorPalaVR controladorPala;

    [Header("Aparición de Pelota")]
    public GameObject prefabPelota;
    public Transform puntoAparicion;

    [Header("Estado de la Partida")]
    public int pelotasEnJuego = 0;
    public int bloquesRestantes = 0;

    void Awake()
    {
        // Al despertar, el Gestor se registra a sí mismo
        Instancia = this;
    }

    void Start()
    {
        textoEstadisticas.text = "";
        StartCoroutine(RutinaInicioPartida());
    }

    IEnumerator RutinaInicioPartida()
    {
        if (listaDeNiveles.Length > 0 && nivelElegido < listaDeNiveles.Length)
        {
            Instantiate(listaDeNiveles[nivelElegido], transform.position, Quaternion.identity, transform);

            // Damos un microsegundo para que los bloques se generen antes de contarlos
            yield return new WaitForEndOfFrame();
            bloquesRestantes = GameObject.FindGameObjectsWithTag("Bloque").Length;
        }

        textoMensajes.text = "3";
        yield return new WaitForSeconds(1f);

        textoMensajes.text = "2";
        yield return new WaitForSeconds(1f);

        textoMensajes.text = "1";
        yield return new WaitForSeconds(1f);

        textoMensajes.text = "¡GO!";
        Instantiate(prefabPelota, puntoAparicion.position, Quaternion.identity, transform);

        yield return new WaitForSeconds(1f);
        textoMensajes.text = "";
        juegoEmpezado = true;
    }

    public void AlternarPausa(bool estaEnPausa)
    {
        // Si el juego no ha empezado (está en el 3,2,1 inicial), no hacemos nada 
        if (!juegoEmpezado) return;

        // Si ya había una cuenta atrás de reanudación en marcha, la cancelamos
        if (rutinaReanudacion != null)
        {
            StopCoroutine(rutinaReanudacion);
        }

        if (estaEnPausa)
        {
            Time.timeScale = 0f; // CONGELAMOS EL TIEMPO
            textoMensajes.text = "PAUSA";
        }
        else
        {
            // Iniciamos la cuenta atrás para descongelar
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
    }

    public void RegistrarPelota() { pelotasEnJuego++; }

    public void PelotaDestruida()
    {
        pelotasEnJuego--;
        if (pelotasEnJuego <= 0 && bloquesRestantes > 0)
        {
            MostrarResultados("¡FIN DEL JUEGO!");
        }
    }

    public void BloqueDestruido()
    {
        bloquesRestantes--;
        if (bloquesRestantes <= 0)
        {
            // Destruir las pelotas que queden
            GameObject[] pelotas = GameObject.FindGameObjectsWithTag("Pelota");
            foreach (GameObject p in pelotas) Destroy(p);

            MostrarResultados("¡NIVEL COMPLETADO!");
        }
    }

    void MostrarResultados(string mensaje)
    {
        textoMensajes.text = mensaje;

        if (controladorPala != null)
        {
            // Formateamos los datos a 2 decimales y usamos Valor Absoluto (Abs) 
            // para que la izquierda no salga en negativo
            string romIzq = Mathf.Abs(controladorPala.maxEstiramientoIzquierda).ToString("F2");
            string romDer = Mathf.Abs(controladorPala.maxEstiramientoDerecha).ToString("F2");

            textoEstadisticas.text = $"RANGO DE MOVIMIENTO (ROM)\nIzquierda: {romIzq} \nDerecha: {romDer}";
        }
    }
}