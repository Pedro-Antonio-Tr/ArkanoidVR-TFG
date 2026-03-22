using UnityEngine;

public class GestorArkanoid : MonoBehaviour
{
    [Header("Estado de la Partida")]
    public int pelotasEnJuego = 0;

    public void RegistrarPelota()
    {
        pelotasEnJuego++;
    }

    public void PelotaDestruida()
    {
        pelotasEnJuego--;

        if (pelotasEnJuego <= 0)
        {
            Debug.Log("¡GAME OVER! No quedan pelotas en pantalla.");
            // Aquí en el futuro mostraremos el menu de reinicio o guardaremos datos del paciente, cuando aprenda a hacerlo y toque el tema de UI.
        }
    }
}
