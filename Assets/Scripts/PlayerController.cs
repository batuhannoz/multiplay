using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float MovementSpeed = 1;
    private float h;   

    Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        h = Input.GetAxisRaw("Horizontal");
        if (h != 0) rb.velocity = new Vector2(h,0) * MovementSpeed * Time.deltaTime;
    }
}
