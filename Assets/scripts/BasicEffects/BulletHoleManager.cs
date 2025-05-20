using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class BulletHoleManager : NetworkBehaviour
{
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private GameObject bulletHoleContainer;
    [SerializeField] private float destroyDelay;

    private Ray _currentRay;
    private RaycastHit _currentHit;
    
    public static BulletHoleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void SpawnBulletHole(RaycastHit hit, Ray ray)
    {
        const float positionMultiplier = 0.8f;
        var spawnX = hit.point.x - ray.direction.x * positionMultiplier;
        var spawnY = hit.point.y - ray.direction.y * positionMultiplier;
        var spawnZ = hit.point.z - ray.direction.z * positionMultiplier;
        var spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

        var spawnedObject = Instantiate(bulletHolePrefab, spawnPosition, Quaternion.identity);
        var targetRotation = Quaternion.LookRotation(ray.direction);

        spawnedObject.transform.rotation = targetRotation;
        //spawnedObject.transform.SetParent(bulletHoleContainer.transform);
        spawnedObject.transform.SetParent(hit.collider.transform);
        spawnedObject.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
        Destroy(spawnedObject, destroyDelay);
    }
    
    public void SpawnBulletHoleRPC(RaycastHit hit, Ray ray, Transform parent)
    {
        if (!IsServer)
        {
            Debug.Log("Not Server, aborting...");
            return;
        }
        
        const float positionMultiplier = 0.8f;
        Vector3 spawnPosition = hit.point - ray.direction * positionMultiplier;

        GameObject spawnedObject = Instantiate(bulletHolePrefab, spawnPosition, Quaternion.identity);
        spawnedObject.transform.rotation = Quaternion.LookRotation(ray.direction);
        spawnedObject.transform.SetParent(parent);
        spawnedObject.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
        Destroy(spawnedObject, destroyDelay);
    }
    
    [ClientRpc]
    public void SpawnBulletHoleClientRpc(Vector3 hitPoint, Vector3 normal, Vector3 direction, ulong parentNetworkObjectId)
    {
        Vector3 spawnPosition = hitPoint - direction.normalized * 0.01f;

        GameObject bulletHole = Instantiate(bulletHolePrefab, spawnPosition, Quaternion.LookRotation(-normal));
        bulletHole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentNetworkObjectId, out var parentObj))
        {
            bulletHole.transform.SetParent(parentObj.transform);
        }

        Destroy(bulletHole, destroyDelay);
    }

}