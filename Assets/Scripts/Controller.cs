using System;
using UnityEngine;

public class Controller : MonoBehaviour
{
    
    public float mouseSensitivityX = 250f;
    public float mouseSensitivityY = 250f;
    public float jumpForce = 220f;
    public LayerMask groundedMask;
    public float walkSpeed = 5f;
    
    Transform cameraTransform;
    
    public float vertLookLocation;
    
    private Vector3 moveAmmount;
    Vector3 SmoothMoveVelocity;
    
    private Rigidbody rb;
    private bool grounded;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * Time.deltaTime* mouseSensitivityX, Space.World);
        
        vertLookLocation += Input.GetAxis("Mouse Y") * Time.deltaTime*mouseSensitivityY;
        vertLookLocation = Mathf.Clamp(vertLookLocation, -60f, 60f);
        
        cameraTransform.localRotation = Quaternion.Euler(vertLookLocation, 0, 0);
        
        Vector3 moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
        Vector3 targetMoMoveAmount = moveDir * walkSpeed;
        
        moveAmmount = Vector3.SmoothDamp(moveAmmount, targetMoMoveAmount, ref SmoothMoveVelocity, 0.15f);

        if (Input.GetButtonDown("Jump"))
        {
            if (grounded) rb.AddForce(transform.up * jumpForce);
        }

        grounded = false;
        
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.1f, groundedMask))
        {
            grounded = true;
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmmount) * Time.fixedDeltaTime);
    }
}
