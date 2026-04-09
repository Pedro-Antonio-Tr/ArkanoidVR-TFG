using UnityEngine;

public class BloqueArkanoid : MonoBehaviour
{
    [Header("Configuración del Bloque")]
    public int puntosDeVida = 1;

    [Tooltip("Pon aquí los materiales. Índice 0 = 1 Vida, Índice 1 = 2 Vidas...")]
    public Material[] materialesPorVida;

    [Header("Mejoras")]
    public GameObject prefabMejoraMultibola;
    [Range(0f, 100f)] public float probabilidadMejora = 5f;

    [Header("Efectos Visuales")]
    public Material materialGolpe;

    private MeshRenderer renderizador;
    private Material materialOriginal;

    [Header("Sonidos")]
    public AudioClip sonidoGolpe;
    public AudioClip sonidoRotura;
    private AudioSource audioSourceLocal;

    void Start()
    {
        renderizador = GetComponent<MeshRenderer>();
        materialOriginal = renderizador.material;
        audioSourceLocal = gameObject.AddComponent<AudioSource>();
        audioSourceLocal.spatialBlend = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pelota"))
        {
            if (MonitorClinico.Instancia != null)
            {
                MonitorClinico.Instancia.IniciarMedicionReaccion();
            }

            puntosDeVida--;

            if (puntosDeVida <= 0)
            {
                if (sonidoRotura != null)
                {
                    GestorArkanoid.Instancia.ReproducirSonidoGlobal(sonidoRotura);
                    Destroy(gameObject);
                }
                FindFirstObjectByType<GestorArkanoid>().BloqueDestruido();
                if (prefabMejoraMultibola != null)
                {
                    float tirada = Random.Range(0f, 100f);

                    float multiProbabilidad = 1f;
                    if (MonitorClinico.Instancia != null)
                    {
                        if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Facil)
                        {
                            multiProbabilidad = 1.5f; 
                        }
                        else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Dificil)
                        {
                            multiProbabilidad = 0.5f; 
                        }
                    }

                    if (tirada <= (probabilidadMejora * multiProbabilidad))
                    {
                        Instantiate(prefabMejoraMultibola, transform.position, Quaternion.Euler(0f, 0f, -45f), transform.parent);
                    }
                }
                Destroy(gameObject);
            }
            else
            {
                if (sonidoGolpe != null)
                {
                    audioSourceLocal.PlayOneShot(sonidoGolpe);
                }
                StartCoroutine(GolpeNoLetal());
            }
        }
    }

    private System.Collections.IEnumerator GolpeNoLetal()
    {

        if (renderizador != null)
        {
            renderizador.material = materialGolpe;
        }

        yield return new WaitForSeconds(0.1f);

        if (renderizador != null)
        {
            renderizador.material = materialOriginal;
        }

        ActualizarColor();
    }

    void ActualizarColor()
    {
        // Comprobamos que hayamos puesto materiales en el Inspector para no dar error
        if (materialesPorVida.Length > 0)
        {
            // Si le queda 1 vida, usa el material 0. Si le quedan 2, el material 1.
            int indiceMaterial = puntosDeVida - 1;

            // Medida de seguridad por si nos equivocamos al poner la cantidad
            if (indiceMaterial >= 0 && indiceMaterial < materialesPorVida.Length)
            {
                renderizador.material = materialesPorVida[indiceMaterial];
            }
        }
    }
}
