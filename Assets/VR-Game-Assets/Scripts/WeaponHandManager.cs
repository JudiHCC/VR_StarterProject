using UnityEngine;

public class WeaponHandManager : MonoBehaviour
{
    public PistolHandGrab pistol;
    public SwordHandGrab sword;

    public bool pistolInRightHand = true;

    public bool allowButtonSwap = true;
    public OVRInput.Button swapButton = OVRInput.Button.Two;

    public AudioClip switchSound;
    [Range(0f, 1f)] public float switchVolume = 1f;

    private AudioSource audioSource;
    private bool appliedPistolInRightHand;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        Apply(false);
    }

    private void Update()
    {
        if (allowButtonSwap && !ScoreManager.Instance.IsGameOver && OVRInput.GetDown(swapButton))
            SwapHands();

        if (pistolInRightHand != appliedPistolInRightHand)
            Apply(true);
    }

    private void Apply(bool playSound)
    {
        appliedPistolInRightHand = pistolInRightHand;

        pistol.SetHand(pistolInRightHand ? WeaponHand.Right : WeaponHand.Left);
        sword.SetHand(pistolInRightHand ? WeaponHand.Left : WeaponHand.Right);

        if (playSound)
            audioSource.PlayOneShot(switchSound, switchVolume);
    }

    public void SwapHands()
    {
        pistolInRightHand = !pistolInRightHand;
        Apply(true);
    }
}
