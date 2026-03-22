using UnityEngine;

public class ComportamientoPelota : MonoBehaviour
{
    public float velocidad = 15f;
    public float limiteInferiorY = -9f; // Altura a la que la pelota "muere" (por debajo de la pala)

    private Rigidbody rb;
    private GestorArkanoid gestor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gestor = FindFirstObjectByType<GestorArkanoid>();

        if (gestor != null) gestor.RegistrarPelota();

        // Lanzar la pelota hacia arriba y en una dirección aleatoria inicial
        Vector3 direccionInicial = new Vector3(Random.Range(-1f, 1f), 1f, 0f).normalized;
        rb.linearVelocity = direccionInicial * velocidad;
    }

    // Update is called once per frame
    void Update()
    {
        // Mantener la pelota estrictamente en z=0 por seguridad
        transform.localPosition = new UnityEngine.Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        // Forzamos la velocidad constante en cada frame para evitar que se ralentice o acelere demasiado por bugs.
        rb.linearVelocity = rb.linearVelocity.normalized * velocidad;

        // Comprobar si ha caído por debajo de la pala
        if (transform.localPosition.y < limiteInferiorY)
        {
            if (gestor != null) gestor.PelotaDestruida();
            Destroy(gameObject);
        }
    }
}
