using UnityEngine;

public class RaycastShot : MonoBehaviour
{
    public float rayDistance = 10f;
    public Camera cam;

    [SerializeField] private InputManager inputManager; // Assign in Inspector

    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.FireEvent += OnFire;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.FireEvent -= OnFire;
        }
    }

    private void OnFire()
    {
        if (cam == null)
            cam = Camera.main;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name + " at distance " + hit.distance);

            if (hit.collider.CompareTag("Target"))
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(Random.value, Random.value, Random.value);
                }
            }
        }
        else
        {
            Debug.Log("No hit within " + rayDistance + " units.");
        }
    }
}
