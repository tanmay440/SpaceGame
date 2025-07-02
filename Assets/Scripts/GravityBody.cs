using System;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
        GravityAttractor planet;
        
        void Awake()
        {
                planet = FindAnyObjectByType<GravityAttractor>();
                Rigidbody rb = this.GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
                planet.Attract(transform);
        }
}
