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

    public bool debugLogs = true;

    private bool appliedPistolInRightHand;
    private Coroutine tempSwap;

    private void Start()
    {
        if (pistol == null)
            pistol = FindFirstObjectByType<PistolHandGrab>();

        if (sword == null)
            sword = FindFirstObjectByType<SwordHandGrab>();

        Apply();
    }

    private void Update()
    {
        if (allowButtonSwap && OVRInput.GetDown(swapButton))
            SwapHands();

        // Si es canvia la casella a l'Inspector durant el joc.
        if (pistolInRightHand != appliedPistolInRightHand)
            Apply();
    }

    private void Apply()
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
    }

    public void SwapHands()
    {
        pistolInRightHand = !pistolInRightHand;
        Apply();
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
