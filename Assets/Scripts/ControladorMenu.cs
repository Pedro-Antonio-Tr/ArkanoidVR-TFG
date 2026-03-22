using UnityEngine;

public class ControladorMenu : MonoBehaviour
{
    [Header("Configuración del Menú")]
    public GameObject panelMenu; // Arrastraremos el Fondo_Menu aquí
    public Transform headAnchor; // Arrastraremos el CenterEyeAnchor del Rig aquí

    [Header("Ajustes Clínicos")]
    public float distanciaDeLaCara = 1.8f; // Puedes probar con 1.0f o 1.5f en el Inspector

    [Header("Puntero Láser")]
    public PunteroLaserVR scriptLaser;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) ||
            OVRInput.GetDown(OVRInput.Button.Three) ||
            OVRInput.GetDown(OVRInput.Button.Start))
        {
            if (panelMenu == null || headAnchor == null) return;

            bool estaActivado = !panelMenu.activeSelf;
            panelMenu.SetActive(estaActivado);
            if (scriptLaser != null) scriptLaser.enabled = estaActivado;

            if (GestorArkanoid.Instancia != null)
            {
                GestorArkanoid.Instancia.AlternarPausa(estaActivado);
            }

            if (estaActivado)
            {
                ColocarMenuDelanteDeLaMirada();
            }
        }
    }

    void ColocarMenuDelanteDeLaMirada()
    {
        UnityEngine.Vector3 headPos = headAnchor.position;
        UnityEngine.Vector3 lookDirection = headAnchor.forward;

        UnityEngine.Vector3 targetPos = headPos + (lookDirection.normalized * distanciaDeLaCara);

        targetPos.y = Mathf.Max(targetPos.y, headPos.y - 0.2f); // Que no baje mucho del nivel del pecho

        transform.position = targetPos;

        transform.LookAt(headPos);
        transform.Rotate(0, 180, 0);
    }
}
