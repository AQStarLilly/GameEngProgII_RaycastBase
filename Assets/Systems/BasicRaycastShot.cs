using UnityEngine;

public class BasicRaycastShot : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float rayDistance = 10f;   // Maximum distance the ray will travel
    public Camera cam;                // Reference to the camera (can assign in inspector, falls back to Camera.main if left empty)

    [Header("References")]
    [SerializeField] private InputManager inputManager; // Reference to the InputManager (drag & drop in Inspector)

    void OnEnable()
    {
        // Subscribe to the Fire event from the InputManager when this script is enabled
        if (inputManager != null)
        {
            inputManager.FireEvent += OnFire;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the Fire event when this script is disabled
        if (inputManager != null)
        {
            inputManager.FireEvent -= OnFire;
        }
    }

    /// <summary>
    /// Called whenever the Fire action is triggered (Left Mouse Button).
    /// Performs a raycast forward from the camera.
    /// </summary>
    private void OnFire()
    {
        // If no camera is set in the inspector, use the main camera
        if (cam == null)
            cam = Camera.main;

        // Create a ray starting at the camera position, pointing forward
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // Perform the raycast
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // Print the name of the object and distance to the console
            Debug.Log("Hit: " + hit.collider.gameObject.name + " at distance " + hit.distance);

            // If the object is tagged as "Target", change its material color randomly
            if (hit.collider.CompareTag("Target"))
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Assign a random color (R, G, B values between 0–1)
                    rend.material.color = new Color(Random.value, Random.value, Random.value);
                }
            }
        }
        else
        {
            // Nothing was hit within the ray distance
            Debug.Log("No hit within " + rayDistance + " units.");
        }
    }
}
