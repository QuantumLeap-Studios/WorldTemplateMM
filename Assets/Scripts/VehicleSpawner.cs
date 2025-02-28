using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public enum Vehicles
    {
        Plane,
        Car
    }

    public Vehicles Vehicle = Vehicles.Car;
}
