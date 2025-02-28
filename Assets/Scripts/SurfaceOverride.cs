using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceOverride : MonoBehaviour
{
    public enum SurfaceOverrides
    {
        Grass,
        Snow,
        Wood,
        Metal,
        Goo,
        WaterBalloon
    }
    public SurfaceOverrides Override;
    public bool LaunchPlayer = false;
    public bool Swimmable = false;
    public float Power = 0f;
    public Vector3 LaunchDirection = Vector3.zero;
}
