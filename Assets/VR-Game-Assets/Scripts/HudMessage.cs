using UnityEngine;
using TMPro;

// Missatge curt que apareix davant dels ulls del jugador i s'esvaeix.
// Es crea sol la primera vegada que es fa servir: HudMessage.Show("Text");
public class HudMessage : MonoBehaviour
{
    // Posició respecte a la càmera (metres): una mica per sota del centre de la vista.
    public static Vector3 offset = new Vector3(0f, -0.12f, 1.3f);
    public static float fontSize = 0.5f;
    public static Color color = new Color(1f, 0.85f, 0.1f);

    private static HudMessage instance;

    private TextMeshPro text;
    private float timeLeft;
    private float duration;

    public static void Show(string message, float seconds = 2f)
    {
        if (instance == null)
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                Debug.LogWarning("[HUD] No hi ha cap càmera amb el tag MainCamera.");
                return;
            }

            GameObject go = new GameObject("HudMessage");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = offset;
            go.transform.localRotation = Quaternion.identity;

            instance = go.AddComponent<HudMessage>();
            instance.text = go.AddComponent<TextMeshPro>();
            instance.text.alignment = TextAlignmentOptions.Center;
            instance.text.fontSize = fontSize;
            instance.text.fontStyle = FontStyles.Bold;
            instance.text.textWrappingMode = TextWrappingModes.NoWrap;
            instance.text.rectTransform.sizeDelta = new Vector2(1.5f, 0.4f);
            instance.text.outlineWidth = 0.2f;
            instance.text.outlineColor = Color.black;
        }

        instance.text.text = message;
        instance.text.color = color;
        instance.duration = Mathf.Max(0.1f, seconds);
        instance.timeLeft = instance.duration;
        instance.gameObject.SetActive(true);
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Els últims 0,5 s s'esvaeix.
        text.alpha = Mathf.Clamp01(timeLeft / 0.5f);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
