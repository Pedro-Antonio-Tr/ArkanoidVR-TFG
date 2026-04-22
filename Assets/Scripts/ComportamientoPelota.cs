using UnityEngine;

public class ComportamientoPelota : MonoBehaviour
{
    public float velocidad = 15f;
    public float limiteInferiorY = -9f; // Altura a la que la pelota "muere" (por debajo de la pala)
    private float velocidadActual;

    private Rigidbody rb;
    private GestorArkanoid gestor;
    private bool lanzada = false;

    [Header("Efectos de Sonido")]
    public AudioClip sonidoPared;
    public AudioClip sonidoPala;
    private AudioSource audioSourceLocal;

    [Header("Visuales Explosivos")]
    public Material matExplosivo;
    private Material matOriginal;
    private MeshRenderer rendererPelota;

    [Header("Ajustes Explosión")]
    public float radioExplosion = 2.5f;
    public GameObject prefabEfectoExplosion;

    void Awake()
    {
        rendererPelota = GetComponent<MeshRenderer>();
        matOriginal = rendererPelota.material;
        rb = GetComponent<Rigidbody>();
        gestor = FindFirstObjectByType<GestorArkanoid>();

        if (gestor != null) gestor.RegistrarPelota();

        audioSourceLocal = gameObject.AddComponent<AudioSource>();
        audioSourceLocal.spatialBlend = 0;
    }

    void Update()
    {
        if (!lanzada) return;

        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        rb.linearVelocity = rb.linearVelocity.normalized * velocidadActual;

        Vector3 vel = rb.linearVelocity;
        Vector3 pos = transform.localPosition;

        // Límites de la pantalla 
        float limiteX = 17.5f;
        float limiteY_Superior = 10f;

        if (pos.x > limiteX)
        {
            vel.x = -Mathf.Abs(vel.x);
            pos.x = limiteX - 0.1f;
        }
        else if (pos.x < -limiteX)
        {
            vel.x = Mathf.Abs(vel.x);
            pos.x = -limiteX + 0.1f;
        }

        if (pos.y > limiteY_Superior)
        {
            vel.y = -Mathf.Abs(vel.y);
            pos.y = limiteY_Superior - 0.1f;
        }

        if (Mathf.Abs(vel.y) < 2f) vel.y = (vel.y >= 0) ? 2f : -2f;
        if (Mathf.Abs(vel.x) < 1.5f) vel.x = (vel.x >= 0) ? 1.5f : -1.5f;

        transform.localPosition = pos;
        rb.linearVelocity = vel;

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
            bool esIzquierda = collision.gameObject.name.Contains("Izquierda");

            if (MonitorClinico.Instancia != null)
            {
                MonitorClinico.Instancia.RegistrarGolpePala(esIzquierda);
            }
            if (sonidoPala != null)
            {
                audioSourceLocal.PlayOneShot(sonidoPala);
            }
        }
        else if (collision.gameObject.CompareTag("Bloque"))
        {
            if (GestorArkanoid.Instancia.explosivoActivo)
            {
                EjecutarExplosion();
            }
        }
    }

    public void ActualizarVisualesExplosivos()
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

    public void Lanzar(Vector3 direccion)
    {
        velocidadActual = velocidad;

        if (MonitorClinico.Instancia != null)
        {
            if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Facil)
            {
                velocidadActual = velocidad * 0.5f;
            }
            else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Normal)
            {
                velocidadActual = velocidad * 0.75f;
            }
            else if (MonitorClinico.Instancia.dificultadActual == MonitorClinico.NivelDificultad.Dificil)
            {
                velocidadActual = velocidad * 1.0f;
            }
        }

        rb.linearVelocity = direccion.normalized * velocidadActual;
        lanzada = true;
    }
}
