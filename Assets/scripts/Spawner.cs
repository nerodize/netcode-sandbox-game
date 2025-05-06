using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    //[SerializeField] GameObject targetContainer;
    
    public GameObject targetPrefab;
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector3 spawnAreaSize = new(10f, 0f, 10f);

    public void Respawn()
    {
        Vector3 randomPosition = GetRandomPositionInArea();
        Instantiate(targetPrefab, randomPosition, Quaternion.identity);
    }

    private Vector3 GetRandomPositionInArea()
    {
        float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
        float z = Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2);
        return spawnAreaCenter + new Vector3(x, 0f, z);
    }
    
    public IEnumerator RespawnDelayed()
    {
        yield return new WaitForSeconds(2f);
        Respawn();
    }

    private void Start()
    {
        Respawn();
    }
}