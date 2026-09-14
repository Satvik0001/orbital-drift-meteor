using UnityEngine;

public class Meteor : MonoBehaviour
{
    public Transform planet;

    public float fallSpeed = 20f;

    public GameObject explosionPrefab;
    public GameObject craterPrefab;

    void Update()
    {
        if (planet == null)
            return;

        // Move towards planet center
        Vector3 direction =
            (planet.position - transform.position).normalized;

        transform.position +=
            direction *
            fallSpeed *
            Time.deltaTime;

        // Spin meteor
        transform.Rotate(
            150f * Time.deltaTime,
            100f * Time.deltaTime,
            50f * Time.deltaTime
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        // If meteor hits player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("GAME OVER");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }

        // Create explosion
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // Create crater when hitting anything
        if (craterPrefab != null && planet != null)
        {
            Vector3 surfaceNormal =
                (transform.position - planet.position).normalized;

            GameObject crater =
                Instantiate(
                    craterPrefab,
                    transform.position -
                    surfaceNormal * 0.5f,
                    Quaternion.identity
                );

            crater.transform.up = surfaceNormal;
        }

        // Destroy meteor
        Destroy(gameObject);
    }
}