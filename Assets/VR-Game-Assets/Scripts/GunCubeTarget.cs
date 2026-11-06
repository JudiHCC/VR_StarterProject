using UnityEngine;
using Autohand;

[RequireComponent(typeof(Rigidbody))]
public class GunCubeTarget : AutoGunTarget
{
    public override void OnShot(AutoGun gun, RaycastHit hit)
    {
        GetComponent<CubeExplode>().BreakCube(hit.point - gun.transform.position);
    }
}
