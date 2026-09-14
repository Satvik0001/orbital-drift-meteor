using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    public Transform planet;
    public float gravityForce = 100f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 gravityDirection =
            (planet.position - transform.position).normalized;

        rb.AddForce(
            gravityDirection * gravityForce,
            ForceMode.Acceleration
        );

        Quaternion targetRotation =
            Quaternion.FromToRotation(
                transform.up,
                -gravityDirection
            ) * transform.rotation;

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }
}