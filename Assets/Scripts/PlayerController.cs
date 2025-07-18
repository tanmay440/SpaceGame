using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public Transform planetCenter;
    
    private Rigidbody rb;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // FIX: Constrain rotation to prevent spinning
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.freezeRotation = true;
            rb.useGravity = false; // We're using custom gravity
        }
        
        // Auto-find planet if not assigned
        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("Planet");
            if (planet != null) planetCenter = planet.transform;
        }
    }
    
    void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position, -transform.up, 1.1f);
        
        // Jumping
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            // FIX: Apply jump force relative to planet center
            Vector3 jumpDirection = (transform.position - planetCenter.position).normalized;
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
        }
    }
    
    void FixedUpdate()
    {
        // Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // FIX: Get movement direction relative to player's orientation
        Vector3 moveDirection = transform.TransformDirection(
            new Vector3(horizontal, 0, vertical)
        ).normalized;
        
        // Apply movement force
        rb.AddForce(moveDirection * moveSpeed);
        
        // FIX: Limit maximum velocity to prevent sliding
        if (rb.velocity.magnitude > moveSpeed)
        {
            rb.velocity = rb.velocity.normalized * moveSpeed;
        }
    }
}