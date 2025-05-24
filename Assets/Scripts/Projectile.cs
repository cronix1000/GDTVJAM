using UnityEngine;

public class Projectile : MonoBehaviour {

    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 1;
    // public GameObject hitEffectPrefab;
    // public string targetTag = "Enemy"; // Or "Player" if fired by an enemy

    private Vector2 _direction;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            Debug.LogError("Projectile prefab needs a Rigidbody2D component!");
            enabled = false; // Disable script if no Rigidbody2D
        }
    }

    public void Initialize(Vector2 direction, float projectileSpeed)
    {
        this._direction = direction.normalized;
        this.speed = projectileSpeed;
        Destroy(gameObject, lifetime);

        // Rotate projectile to face direction of travel
        if (_direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f; // -90 if sprite points 'up'
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // add a timer for it 
        Invoke("DestroyProjectile", 3.0f); 
    }

    void FixedUpdate()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = _direction * speed;
        }
    }

    void DestroyProjectile()
    {
        // if (hitEffectPrefab) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

}