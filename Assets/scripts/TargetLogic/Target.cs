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

    // NetworkVariable für Position (default WritePermission = Server)
    private NetworkVariable<Vector3> _networkPosition = new(
        writePerm: NetworkVariableWritePermission.Server);

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
        if (IsServer && _isMoving)
        {
            transform.Translate(_moveDirection * (_moveSpeed * Time.deltaTime));
            // Position in NetworkVariable schreiben, damit Clients es bekommen
            _networkPosition.Value = transform.position;
        }
        else if (!IsServer)
        {
            // Auf Clients Position aus NetworkVariable lesen
            transform.position = _networkPosition.Value;
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
            NetworkObject.Despawn(); // Nur der Server darf despawnen
        }
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
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
