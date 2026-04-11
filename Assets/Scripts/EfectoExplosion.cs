using UnityEngine;
using System.Collections;

public class EfectoExplosion : MonoBehaviour
{
    [Header("Ajustes")]
    public float tiempoDuracion = 0.5f; 
    public float multiplicadorEscala = 3f; 

    private SpriteRenderer spriteRenderer;
    private Vector3 escalaInicial;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        escalaInicial = transform.localScale;
        StartCoroutine(RutinaExplosion());
    }

    private IEnumerator RutinaExplosion()
    {
        float tiempoTranscurrido = 0f;
        Color colorActual = spriteRenderer.color;

        while (tiempoTranscurrido < tiempoDuracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoDuracion;

            transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * multiplicadorEscala, progreso);

            if (spriteRenderer != null)
            {
                colorActual.a = Mathf.Lerp(1f, 0f, progreso);
                spriteRenderer.color = colorActual;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}