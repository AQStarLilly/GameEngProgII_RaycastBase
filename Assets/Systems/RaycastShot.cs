using UnityEngine;

public class RaycastShot : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float rayDistance = 10f;
    public Camera cam;

    [Header("References")]
    [SerializeField] private InputManager inputManager;

    [Header("Highlight Settings")]
    private Renderer currentHighlight;   // The object currently highlighted
    private Color storedBaseColor;       // The object's permanent base color
    public Color highlightColor = Color.yellow; // Temporary highlight color

    void OnEnable()
    {
        if (inputManager != null)
            inputManager.FireEvent += OnFire;
    }

    void OnDisable()
    {
        if (inputManager != null)
            inputManager.FireEvent -= OnFire;
    }

    void Update()
    {
        DoHoverHighlight();
    }

    /// <summary>
    /// Called whenever the Fire action is triggered (Left Mouse Button).
    /// Randomizes color if the hit object is a Target.
    /// </summary>
    private void OnFire()
    {
        if (cam == null)
            cam = Camera.main;

        RaycastHit? hit = GetFilteredHit();

        if (hit.HasValue)
        {
            Debug.Log("Hit: " + hit.Value.collider.gameObject.name + " at distance " + hit.Value.distance);

            if (hit.Value.collider.CompareTag("Target"))
            {
                Renderer rend = hit.Value.collider.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Set new permanent base color
                    Color newColor = new Color(Random.value, Random.value, Random.value);
                    rend.material.color = newColor;

                    // If this is currently highlighted, update storedBaseColor
                    if (rend == currentHighlight)
                    {
                        storedBaseColor = newColor;
                    }
                }
            }
        }
        else
        {
            Debug.Log("No valid hit within " + rayDistance + " units.");
        }
    }

    /// <summary>
    /// Continuously checks what object is under the crosshair and highlights it.
    /// </summary>
    private void DoHoverHighlight()
    {
        if (cam == null)
            cam = Camera.main;

        RaycastHit? hit = GetFilteredHit();

        if (hit.HasValue)
        {
            Renderer rend = hit.Value.collider.GetComponent<Renderer>();

            if (rend != null)
            {
                if (currentHighlight != rend)
                {
                    ClearHighlight(); // remove highlight from old object

                    // Store the object's current permanent base color
                    currentHighlight = rend;
                    storedBaseColor = rend.material.color;

                    // Apply highlight
                    rend.material.color = highlightColor;
                }
                return;
            }
        }

        // No valid hit - clear highlight
        ClearHighlight();
    }

    /// <summary>
    /// Performs a RaycastAll and returns the first valid hit.
    /// </summary>
    private RaycastHit? GetFilteredHit()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ignore"))
                continue;

            return hit; // return first non-ignored hit
        }

        return null;
    }

    /// <summary>
    /// Restores the last highlighted object back to its base color.
    /// </summary>
    private void ClearHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.material.color = storedBaseColor;
            currentHighlight = null;
        }
    }
}
