using UnityEngine;
using System.Collections;
public class Target : MonoBehaviour, IDamageable
{
    private float _health = 100f;

    private Vector3 _moveDirection;
    private float _moveSpeed;
    private bool _isMoving;
    


    private void Start()
    {
        StartCoroutine(MoveRoutine());
    }
    private void Update()
    {
        if(_isMoving) 
            transform.Translate(_moveDirection * (_moveSpeed * Time.deltaTime));
    }

    public void Damage(float damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator MoveRoutine()
    {
        // geht nur im Loop?
        while (true)
        {
            // --- Phase 1: Vorwärts Bewegung ---
            float randomDistance = Random.Range(1f, 3f);
            float randomSpeed = Random.Range(2f, 5f);
            float direction = Random.value > 0.5f ? 1f : -1f; // Links oder Rechts zufällig

            _moveDirection = new Vector3(direction, 0, 0);
            _moveSpeed = randomSpeed;
            _isMoving = true;

            float moveTime = randomDistance / randomSpeed; // Zeit = Strecke / Geschwindigkeit
            yield return new WaitForSeconds(moveTime);

            // --- Phase 2: Stoppen ---
            _isMoving = false;
            float waitTime = Random.Range(0.5f, 2f); // Zufälliges Warten
            yield return new WaitForSeconds(waitTime);

            // --- Phase 3: Zurück Bewegung ---
            _moveDirection = new Vector3(-direction, 0, 0); // In die entgegengesetzte Richtung
            randomDistance = Random.Range(1f, 3f);
            randomSpeed = Random.Range(2f, 5f);
            _moveSpeed = randomSpeed;
            _isMoving = true;

            moveTime = randomDistance / randomSpeed;
            yield return new WaitForSeconds(moveTime);

            // --- Wieder Stoppen ---
            _isMoving = false;
            waitTime = Random.Range(0.5f, 2f);
            yield return new WaitForSeconds(waitTime);
            // Danach beginnt die Schleife von vorne
        }
    }
}
