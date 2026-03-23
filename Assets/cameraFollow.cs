using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Kogo mamy śledzić (nasz gracz)
    public float smoothSpeed = 5f; // Prędkość "płynnego" doganiania
    public Vector3 offset = new Vector3(0, 0, -10f); // -10 w osi Z jest OBOWIĄZKOWE w 2D!

    void LateUpdate()
    {
        if (target != null)
        {
            // Obliczamy docelową pozycję
            Vector3 desiredPosition = target.position + offset;
            // Płynnie przesuwamy kamerę (Lerp)
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}