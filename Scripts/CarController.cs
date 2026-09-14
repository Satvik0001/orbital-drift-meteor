using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float turnSpeed = 80f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float move = 0f;
        float turn = 0f;

        if (Keyboard.current.wKey.isPressed)
            move = 1f;

        if (Keyboard.current.sKey.isPressed)
            move = -1f;

        if (Keyboard.current.aKey.isPressed)
            turn = -1f;

        if (Keyboard.current.dKey.isPressed)
            turn = 1f;

        // Move
        if (move != 0)
        {
            rb.MovePosition(
                rb.position +
                transform.forward *
                move *
                moveSpeed *
                Time.fixedDeltaTime
            );
        }
        else
        {
            // Kill any sliding
            rb.linearVelocity = Vector3.zero;
        }

        // Turn
        transform.Rotate(
            0f,
            turn * turnSpeed * Time.fixedDeltaTime,
            0f
        );
    }
}