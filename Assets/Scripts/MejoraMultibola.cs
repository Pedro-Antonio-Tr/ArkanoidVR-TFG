using UnityEngine;

public class MejoraMultibola : MonoBehaviour
{
    public float velocidadCaida = 5f;
    public float limiteInferiorY = -10f;
    public AudioClip sonidoMejora;

    void Update()
    {
        transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime, Space.World);

        if (transform.localPosition.y < limiteInferiorY)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pala"))
        {
            if (GestorArkanoid.Instancia != null)
            {
                GestorArkanoid.Instancia.DuplicarPelotas();
            }
            if (sonidoMejora != null)
            {
                AudioSource.PlayClipAtPoint(sonidoMejora, transform.position);
            }
            Destroy(gameObject);
        }
    }
}