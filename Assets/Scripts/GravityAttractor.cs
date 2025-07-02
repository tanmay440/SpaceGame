using UnityEngine;

public class GravityAttractor : MonoBehaviour
{
    public float gravity = -10f;

    // called by all GravityBody objects, simulates gravity as if this object is a planet
    public void Attract(Transform body)
    {
        Vector3 targetDirection = (body.position - transform.position).normalized;
        Vector3 bodyUp = body.up;
        
        body.rotation = Quaternion.FromToRotation(bodyUp, targetDirection) * body.rotation;
        body.GetComponent<Rigidbody>().AddForce(targetDirection * gravity);
    }
}
