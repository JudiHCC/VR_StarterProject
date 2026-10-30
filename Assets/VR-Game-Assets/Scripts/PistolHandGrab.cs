// La pistola va per defecte a la mà dreta.
public class PistolHandGrab : HandGrabBase
{
    protected override WeaponHand DefaultHand => WeaponHand.Right;
    protected override string LogTag => "[Pistol]";
}
