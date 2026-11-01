using UnityEngine;
using UnityEngine.UI;

// Botó de la UI que es prem disparant-li amb la pistola.
// (L'escena no té punter VR per fer clic a la UI.)
// Necessita un collider; ScoreManager el crea sol per als botons del panell final.
[RequireComponent(typeof(Collider))]
public class ShootableButton : MonoBehaviour
{
    public Button button;
    public Color pressedColor = new Color(0.4f, 1f, 0.5f, 1f);

    // Moment (Time.time) a partir del qual es pot prémer,
    // per no prémer-lo sense voler amb els últims trets de la partida.
    public float activeAfter = 0f;

    private bool pressed = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    // Cridat per PistolShooter quan el tret toca el botó.
    public void Press()
    {
        if (pressed || button == null || !button.interactable || Time.time < activeAfter)
            return;

        pressed = true;

        if (button.targetGraphic != null)
            button.targetGraphic.CrossFadeColor(pressedColor, 0.05f, true, true);

        button.onClick.Invoke();
    }
}
