using UnityEngine;

public class MejoraExplosiva : MonoBehaviour
{
    [Header("Ajustes de Caída")]
    public float velocidadCaida = 5f;
    public float limiteInferiorY = -10f;

    [Header("Audio")]
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
                GestorArkanoid.Instancia.ActivarExplosivo();

                if (sonidoMejora != null)
                {
                    GestorArkanoid.Instancia.ReproducirSonidoGlobal(sonidoMejora);
                }
            }
            Destroy(gameObject);
        }
    }
}