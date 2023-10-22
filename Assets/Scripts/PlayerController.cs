using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] public float speed = 100;

    [SerializeField] public Rigidbody2D rb;

    public void Update()
    {
        if (IsOwner) {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            rb.velocity = new Vector2(h, v) * Time.deltaTime * speed;
        }
    }
}

