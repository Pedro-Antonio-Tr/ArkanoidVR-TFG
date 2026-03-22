using UnityEngine;

public class ControladorPalaVR : MonoBehaviour
{
    [Header("Configuración")]
    public float multiplicadorVelocidad = 3f;
    public float limiteIzquierdo = -8f;
    public float limiteDerecho = 8f;

    [Header("Datos Clínicos del Paciente (ROM)")] // A configurar según lo que se pida más adelante
    public float maxEstiramientoIzquierda = 0f;
    public float maxEstiramientoDerecha = 0f;

    void Update()
    {
        // RTouch significa "Right Touch Controller"
        Vector3 posicionMando = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        float nuevaPosicionX = posicionMando.x * multiplicadorVelocidad;

        // Que no se salga de la pantalla
        nuevaPosicionX = Mathf.Clamp(nuevaPosicionX, limiteIzquierdo, limiteDerecho);

        // Mover la pala (mantenemos su Y y Z originales)
        transform.position = new Vector3(nuevaPosicionX, transform.position.y, transform.position.z);

        if (transform.position.x < maxEstiramientoIzquierda) maxEstiramientoIzquierda = transform.position.x;
        if (transform.position.x > maxEstiramientoDerecha) maxEstiramientoDerecha = transform.position.x;
    }
}