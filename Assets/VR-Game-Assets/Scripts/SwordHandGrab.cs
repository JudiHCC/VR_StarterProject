// L'espasa va per defecte a la mà esquerra.
public class SwordHandGrab : HandGrabBase
{
    protected override WeaponHand DefaultHand => WeaponHand.Left;
    protected override string LogTag => "[Sword]";

    protected override void Awake()
    {
        // L'espasa ja no ha de crear-ne una altra quan toca la mà.
        SwordCubePickup pickup = GetComponent<SwordCubePickup>();

        if (pickup != null)
            Destroy(pickup);

        base.Awake();
    }
}
