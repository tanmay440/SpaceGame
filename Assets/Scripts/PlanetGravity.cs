using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    public Transform planetCenter;
    public float gravityForce = 9.81f;
    public float rotationSpeed = 5f;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // FIX: Constrain rotation to prevent spinning
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.freezeRotation = true;
        }
        
        // Auto-find planet if not assigned
        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("Planet");
            if (planet != null) planetCenter = planet.transform;
        }
    }
    
    void FixedUpdate()
    {
        if (planetCenter == null) return;
        
        Vector3 gravityDirection = (transform.position - planetCenter.position).normalized;
        
        // Apply gravity force
        if (rb != null)
        {
            rb.AddForce(-gravityDirection * gravityForce);
        }
        
        // FIX: Only align rotation if we're grounded
        if (Physics.Raycast(transform.position, -transform.up, 1.1f))
        {
            AlignToSurface(gravityDirection);
        }
    }
    
    // FIX: Separated rotation alignment into its own method
    private void AlignToSurface(Vector3 gravityDirection)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravityDirection) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}