
using UnityEngine;
using Autohand;

[RequireComponent(typeof(Rigidbody))]
public class GunCubeTarget : AutoGunTarget
{
    private bool broken = false;

    public override void OnShot(AutoGun gun, RaycastHit hit)
    {
        if (broken)
            return;

        broken = true;

        CubeExplode cube = GetComponentInParent<CubeExplode>();

        if (cube != null)
        {
            cube.BreakCube();
        }
        else
        {
            Debug.LogWarning(
                "GunCubeTarget: no he trobat CubeExplode al cub.",
                this
            );
        }
    }
}
