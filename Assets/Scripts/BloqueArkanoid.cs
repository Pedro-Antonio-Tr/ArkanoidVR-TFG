using UnityEngine;

public class BloqueArkanoid : MonoBehaviour
{
    [Header("Configuración del Bloque")]
    public int puntosDeVida = 1;

    [Tooltip("Pon aquí los materiales. Índice 0 = 1 Vida, Índice 1 = 2 Vidas...")]
    public Material[] materialesPorVida;

    private MeshRenderer renderizador;

    void Start()
    {
        renderizador = GetComponent<MeshRenderer>();
        ActualizarColor();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pelota"))
        {
            puntosDeVida--;

            if (puntosDeVida <= 0)
            {
                FindFirstObjectByType<GestorArkanoid>().BloqueDestruido();
                Destroy(gameObject);
            }
            else
            {
                ActualizarColor();
            }
        }
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
