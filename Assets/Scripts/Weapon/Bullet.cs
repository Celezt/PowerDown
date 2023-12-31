using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

[HideMonoScript, RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bullet : MonoBehaviour
{
    public string TeamTag
    {
        get => _teamTag;
        set => _teamTag = value;
    }
    public ObjectPool<Bullet> Pool
    {
        get => _pool;
        set => _pool = value;
    }

    [SerializeField]
    private float _lifeTime = 3;
    [SerializeField]
    private float _speed = 10;
    [SerializeReference, PropertySpace(SpaceBefore = 8)]
    private List<IEffectBase> _effects;

    private float _spawnTime;
    private string _teamTag;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private ObjectPool<Bullet> _pool;
 
    public void IgnoreCollision(Collider ignoreCollider)
    {
        if (_collider == null)
            _collider = GetComponent<Collider>();

        Physics.IgnoreCollision(ignoreCollider, _collider);
    }

    public void IgnoreCollisions(IReadOnlyList<Collider> ignoreColliders)
    {
        if (ignoreColliders == null)
            return;

        for (int i = 0; i < ignoreColliders.Count; i++)
            IgnoreCollision(ignoreColliders[i]);
    }

    public void Shoot(Vector3 position, Quaternion rotation)
    {
        _spawnTime = Time.time;

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.position = position;
        _rigidbody.rotation = rotation;
        _rigidbody.velocity = Vector3.zero;

        _rigidbody.AddRelativeForce(new Vector3(_speed, 0, 0), ForceMode.VelocityChange);
    }

    private void Update()
    {
        float currentTime = Time.time;
        if (currentTime - _spawnTime > _lifeTime)
        {
            // Destroy or release itself when the lifetime has run out.
            if (_pool == null)
                Destroy(gameObject);
            else
                _pool.Release(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_teamTag != null && other.CompareTag(_teamTag)) // Ignore if target is in the same team.
            return;

        if (other.TryGetComponent(out IEffector effector)) // If effector exist on the object.
            effector.AddEffects(_effects, gameObject);

        // Destroy or release itself when hitting something included in the physics layer.
        if (_pool == null)
            Destroy(gameObject);
        else
            _pool.Release(this); 
    }
}
