using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class Spawner : NetworkBehaviour
{
    //[SerializeField] GameObject targetContainer;
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject targetPrefab;
    
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector3 spawnAreaSize = new(10f, 0f, 10f);

    private void Respawn()
    {
        Vector3 randomPosition = GetRandomPositionInArea();
        
       var targetInstance = Instantiate(targetPrefab, randomPosition, Quaternion.identity);
       targetInstance.GetComponent<NetworkObject>().Spawn();
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

    /*
    private void Start()
    {
        Respawn();
    }
    */
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Respawn();
        }
    }
}