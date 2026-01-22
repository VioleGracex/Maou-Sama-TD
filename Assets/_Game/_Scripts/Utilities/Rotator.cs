using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    public Vector3 rotationSpeed = new Vector3(0, 30, 0);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
