using UnityEngine;

public class BasicRaycastDemo : MonoBehaviour
{
    public Camera cam;
    void Update()
    {
        // Casts a ray forward from the camera/player position
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 20f))
        {
            Debug.Log("Hit: " + hit.collider.name);

            // Optional: draw the ray in the Scene view (red if hit, green if not)
            Debug.DrawRay(cam.transform.position, cam.transform.forward * hit.distance, Color.red);
        }
        else
        {
            Debug.DrawRay(cam.transform.position, cam.transform.forward * 20f, Color.green);
        }
    }
}

