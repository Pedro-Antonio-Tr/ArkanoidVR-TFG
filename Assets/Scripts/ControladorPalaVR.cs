using UnityEngine;

public class ControladorPalaVR : MonoBehaviour
{
    [Header("Modelos Físicos de las Palas")]
    public GameObject palaDerechaObj;
    public GameObject palaIzquierdaObj;

    [Header("Configuración")]
    public float multiplicadorVelocidad = 27.5f;
    public float limiteIzquierdo = -16f;
    public float limiteDerecho = 16f;

    [Header("Datos Clínicos del Paciente (ROM)")]
    public float maxEstiramientoIzquierda = 0f;
    public float maxEstiramientoDerecha = 0f;

    // Guardamos la posición Y y Z originales para que no se muevan hacia arriba o abajo
    private float posYBaseDer = 0f, posZBaseDer = 0f;
    private float posYBaseIzq = 0f, posZBaseIzq = 0f;

    void Start()
    {
        if (palaDerechaObj != null)
        {
            posYBaseDer = palaDerechaObj.transform.localPosition.y;
            posZBaseDer = palaDerechaObj.transform.localPosition.z;
        }
        if (palaIzquierdaObj != null)
        {
            posYBaseIzq = palaIzquierdaObj.transform.localPosition.y;
            posZBaseIzq = palaIzquierdaObj.transform.localPosition.z;
        }
    }

    void Update()
    {
        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null)
        {
            modo = MonitorClinico.Instancia.modoActual;
        }

        if (palaDerechaObj != null)
            palaDerechaObj.SetActive(modo == MonitorClinico.ModoControl.Derecho || modo == MonitorClinico.ModoControl.Ambos);

        if (palaIzquierdaObj != null)
            palaIzquierdaObj.SetActive(modo == MonitorClinico.ModoControl.Izquierdo || modo == MonitorClinico.ModoControl.Ambos);

        if (palaDerechaObj != null && palaDerechaObj.activeSelf)
        {
            Vector3 posMandoDer = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            float posXDer = Mathf.Clamp(posMandoDer.x * multiplicadorVelocidad, limiteIzquierdo, limiteDerecho);

            palaDerechaObj.transform.localPosition = new Vector3(posXDer, posYBaseDer, posZBaseDer);
            RegistrarROM(posXDer);
        }

        if (palaIzquierdaObj != null && palaIzquierdaObj.activeSelf)
        {
            Vector3 posMandoIzq = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            float posXIzq = Mathf.Clamp(posMandoIzq.x * multiplicadorVelocidad, limiteIzquierdo, limiteDerecho);

            palaIzquierdaObj.transform.localPosition = new Vector3(posXIzq, posYBaseIzq, posZBaseIzq);
            RegistrarROM(posXIzq);
        }
    }

    void RegistrarROM(float posX)
    {
        if (posX < maxEstiramientoIzquierda)
        {
            maxEstiramientoIzquierda = posX;
        }
        if (posX > maxEstiramientoDerecha)
        {
            maxEstiramientoDerecha = posX;
        }
    }
}