using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Target : NetworkBehaviour, IDamageable
{
    private float _health = 100f;
    private Spawner _spawner;

    private Vector3 _moveDirection;
    private float _moveSpeed;
    private bool _isMoving;

    private void Start()
    {
        _spawner = FindFirstObjectByType<Spawner>();
        
        if (IsServer)
        {
            StartCoroutine(MoveRoutine());
        }
    }

    private void Update()
    {
        // Bewegung wird nur auf dem Server durchgeführt
        if (IsServer && _isMoving)
        {
            transform.Translate(_moveDirection * (_moveSpeed * Time.deltaTime));
        }
    }

    public void Damage(float damage)
    {
        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            DamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamageServerRpc(float damage)
    {
        ApplyDamage(damage);
    }

    private void ApplyDamage(float damage)
    {
        _health -= damage;

        if (_health <= 0f)
        {
            _spawner?.StartCoroutine(_spawner.RespawnDelayed());

            // Nur der Server darf Objekte zerstören
            NetworkObject.Despawn(); // NICHT Destroy!
        }
    }

    private IEnumerator MoveRoutine()
    {
        //TODO: maybe better code for this bit
        while (true)
        {
            // Bewegung in eine zufällige Richtung
            float distance = Random.Range(1f, 3f);
            float speed = Random.Range(2f, 5f);
            float direction = Random.value > 0.5f ? 1f : -1f;

            _moveDirection = new Vector3(direction, 0, 0);
            _moveSpeed = speed;
            _isMoving = true;

            float moveTime = distance / speed;
            yield return new WaitForSeconds(moveTime);

            _isMoving = false;
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));

            // Zurück in Gegenrichtung
            _moveDirection = new Vector3(-direction, 0, 0);
            distance = Random.Range(1f, 3f);
            speed = Random.Range(2f, 5f);
            _moveSpeed = speed;
            _isMoving = true;

            moveTime = distance / speed;
            yield return new WaitForSeconds(moveTime);

            _isMoving = false;
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
        }
    }
}
