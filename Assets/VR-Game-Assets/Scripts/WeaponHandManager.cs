using UnityEngine;
using System.Collections;

// Decideix a quina mà va la pistola i a quina l'espasa,
// i permet intercanviar-les en temps d'execució.
public class WeaponHandManager : MonoBehaviour
{
    public PistolHandGrab pistol;
    public SwordHandGrab sword;

    // Es pot canviar des de l'Inspector mentre es juga.
    public bool pistolInRightHand = true;

    // Botó per intercanviar les mans durant el joc (B del comandament dret).
    public bool allowButtonSwap = true;
    public OVRInput.Button swapButton = OVRInput.Button.Two;

    [Header("So en canviar de mà")]
    // So que sona quan les armes s'intercanvien (switchSound).
    public AudioClip switchSound;
    [Range(0f, 1f)] public float switchVolume = 1f;

    private AudioSource audioSource;

    public bool debugLogs = true;

    private bool appliedPistolInRightHand;
    private Coroutine tempSwap;

    private void Start()
    {
        if (pistol == null)
            pistol = FindFirstObjectByType<PistolHandGrab>();

        if (sword == null)
            sword = FindFirstObjectByType<SwordHandGrab>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // So "a l'orella", no posicional.

        if (switchSound == null)
            Debug.LogWarning("[Weapons] Falta assignar el Switch Sound.");

        Apply(false);
    }

    private void Update()
    {
        // Quan s'acaba la partida, el botó B serveix per tornar al lobby.
        bool gameOver = ScoreManager.Instance != null && ScoreManager.Instance.IsGameOver;

        if (allowButtonSwap && !gameOver && OVRInput.GetDown(swapButton))
            SwapHands();

        // Si es canvia la casella a l'Inspector durant el joc.
        if (pistolInRightHand != appliedPistolInRightHand)
            Apply(true);
    }

    // playSound: fals a l'inici de la partida (no ha canviat res).
    private void Apply(bool playSound)
    {
        appliedPistolInRightHand = pistolInRightHand;

        WeaponHand pistolHand = pistolInRightHand ? WeaponHand.Right : WeaponHand.Left;
        WeaponHand swordHand = pistolInRightHand ? WeaponHand.Left : WeaponHand.Right;

        if (pistol != null)
            pistol.SetHand(pistolHand);

        if (sword != null)
            sword.SetHand(swordHand);

        if (debugLogs)
            Debug.Log("[Weapons] Pistola a la mà " + (pistolInRightHand ? "dreta" : "esquerra") + ".");

        if (playSound && switchSound != null && audioSource != null)
            audioSource.PlayOneShot(switchSound, switchVolume);
    }

    public void SwapHands()
    {
        pistolInRightHand = !pistolInRightHand;
        Apply(true);
    }

    // Per al cub de l'onada 3: intercanvia les mans durant uns segons.
    public void SwapHandsForSeconds(float seconds)
    {
        if (tempSwap != null)
        {
            StopCoroutine(tempSwap);
            SwapHands();
        }

        tempSwap = StartCoroutine(TempSwap(seconds));
    }

    private IEnumerator TempSwap(float seconds)
    {
        SwapHands();
        yield return new WaitForSeconds(seconds);
        SwapHands();
        tempSwap = null;
    }
}
