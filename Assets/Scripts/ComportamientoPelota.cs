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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pared"))
        {
            if (sonidoPared != null)
            {
                AudioSource.PlayClipAtPoint(sonidoPared, transform.position);
            }
        }
        else if (collision.gameObject.CompareTag("Pala"))
        {
            if (sonidoPala != null)
            {
                AudioSource.PlayClipAtPoint(sonidoPala, transform.position);
            }
        }
    }
}
