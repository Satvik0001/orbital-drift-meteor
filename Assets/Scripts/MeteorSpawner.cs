using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    public GameObject meteorPrefab;
    public Transform planet;

    public float spawnDistance = 40f;
    public float targetRadius = 5f;

    private float timer;
    private float spawnTimer;

    private int meteorsPerWave = 1;

    void Update()
    {
        timer += Time.deltaTime;

        // Increase difficulty every 30 seconds
        meteorsPerWave =
            1 + Mathf.FloorToInt(timer / 30f);

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= 2f)
        {
            spawnTimer = 0f;

            for (int i = 0; i < meteorsPerWave; i++)
            {
                SpawnMeteor();
            }
        }
    }

    void SpawnMeteor()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        Vector3 targetPosition =
            player.transform.position +
            Random.insideUnitSphere * targetRadius;

        Vector3 spawnPosition =
            targetPosition.normalized * spawnDistance;

        GameObject meteor =
            Instantiate(
                meteorPrefab,
                spawnPosition,
                Quaternion.identity
            );

        Meteor meteorScript =
            meteor.GetComponent<Meteor>();

        if (meteorScript != null)
        {
            meteorScript.planet = planet;
            
            meteorScript.explosionPrefab =
                meteorPrefab.GetComponent<Meteor>().explosionPrefab;
        }
    }
}