using UnityEngine;

public class ComportamientoPelota : MonoBehaviour
{
    public float velocidad = 15f;
    public float limiteInferiorY = -9f; // Altura a la que la pelota "muere" (por debajo de la pala)

    private Rigidbody rb;
    private GestorArkanoid gestor;

    [Header("Efectos de Sonido")]
    public AudioClip sonidoPared;
    public AudioClip sonidoPala;
    private AudioSource audioSourceLocal;

    [Header("Visuales Explosivos")]
    public Material matExplosivo;
    private Material matOriginal;
    private MeshRenderer rendererPelota;

    [Header("Ajustes Explosión")]
    public float radioExplosion = 5f;
    public GameObject prefabEfectoExplosion;

    void Start()
    {
        rendererPelota = GetComponent<MeshRenderer>();
        matOriginal = rendererPelota.material;
        rb = GetComponent<Rigidbody>();
        gestor = FindFirstObjectByType<GestorArkanoid>();

        if (gestor != null) gestor.RegistrarPelota();

        // Lanzar la pelota hacia arriba y en una dirección aleatoria inicial
        Vector3 direccionInicial = new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized;
        float velocidadFinal = velocidad;

        if (MonitorClinico.Instancia != null)
        {
            if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Facil)
            {
                velocidadFinal *= 0.75f; // 25% más lenta
            }
            else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Dificil)
            {
                velocidadFinal *= 1.3f; // 30% más rápida
            }
        }
        rb.linearVelocity = direccionInicial * velocidadFinal;
        audioSourceLocal = gameObject.AddComponent<AudioSource>();
        audioSourceLocal.spatialBlend = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Mantener la pelota estrictamente en z=0 por seguridad
        transform.localPosition = new UnityEngine.Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        // Forzamos la velocidad constante en cada frame para evitar que se ralentice o acelere demasiado por bugs.
        rb.linearVelocity = rb.linearVelocity.normalized * velocidad;

        // SISTEMA ANTI-SOFTLOCK
        UnityEngine.Vector3 velActual = rb.linearVelocity;

        // Comprobamos si la velocidad vertical (Y) es peligrosamente baja (ej. menor a 2)
        if (Mathf.Abs(velActual.y) < 2f)
        {
            // Le damos un "empujoncito" artificial de 2 unidades en la dirección que ya llevaba
            // (Si iba un poco hacia arriba, la subimos más; si iba hacia abajo, la bajamos más)
            velActual.y = (velActual.y >= 0) ? 2f : -2f;
            rb.linearVelocity = velActual;
        }

        // Comprobar si ha caído por debajo de la pala
        if (transform.localPosition.y < limiteInferiorY)
        {
            if (gestor != null) gestor.PelotaDestruida();
            Destroy(gameObject);
        }
        ActualizarVisualesExplosivos();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pared"))
        {
            if (sonidoPared != null)
            {
                if (GestorArkanoid.Instancia.explosivoActivo)
                {
                    EjecutarExplosion();
                }
                else
                {
                    audioSourceLocal.PlayOneShot(sonidoPared);
                }
            }
        }
        else if (collision.gameObject.CompareTag("Pala"))
        {
            if (sonidoPala != null)
            {
                if (GestorArkanoid.Instancia.explosivoActivo)
                {
                    EjecutarExplosion();
                }
                else
                {
                    audioSourceLocal.PlayOneShot(sonidoPala);
                }
            }
        }
    }

    void ActualizarVisualesExplosivos()
    {
        if (GestorArkanoid.Instancia.explosivoActivo)
        {
            float tiempo = GestorArkanoid.Instancia.tiempoExplosivoRestante;

            if (tiempo > 1.5f)
            {
                rendererPelota.material = matExplosivo;
            }
            else // Parpadeo final (cada vez más rápido)
            {
                float velocidadParpadeo = Mathf.Lerp(15f, 2f, tiempo / 1.5f);
                rendererPelota.material = (Mathf.Sin(Time.time * velocidadParpadeo) > 0) ? matExplosivo : matOriginal;
            }
        }
        else
        {
            rendererPelota.material = matOriginal;
        }
    }

    void EjecutarExplosion()
    {
        GestorArkanoid.Instancia.ReproducirSonidoGlobal(GestorArkanoid.Instancia.sonidoExplosion);

        if (prefabEfectoExplosion != null)
        {
            Instantiate(prefabEfectoExplosion, transform.position, Quaternion.identity);
        }

        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider col in objetosCercanos)
        {
            if (col.CompareTag("Bloque"))
            {
                BloqueArkanoid bloque = col.GetComponent<BloqueArkanoid>();
                if (bloque != null)
                {
                    bloque.RecibirDanoExplosion();
                }
            }
        }
    }
}
