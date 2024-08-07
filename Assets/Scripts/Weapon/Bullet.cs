using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

[HideMonoScript, RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bullet : MonoBehaviour, IEntity
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
    public Rigidbody Rigidbody
    {
        get
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            return _rigidbody;
        }
    }

    [SerializeReference]
    private ILifetime _lifeTime = new StaticLifetime();
    [SerializeReference, Space(8)]
    private IHit _hit = new DestroyHit();
    [SerializeReference, Space(8)]
    private ITrajectory _trajectory = new LinearTrajectory();
    [SerializeReference, Space(8)]
    private List<IEffect> _effects;

    [SerializeField, Space(8)]
    private UnityEvent _onShootEvent;

    [SerializeField]
    private UnityEvent _onDeactivateEvent;

    private string _teamTag;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private ObjectPool<Bullet> _pool;
    private CancellationTokenSource _cancellationTokenSource;

    void IEntity.Destroy()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            return;

        // Destroy or release itself when the lifetime has run out.
        if (_pool == null)
            Destroy(gameObject);
        else
            _pool.Release(this);

        CTSUtility.Clear(ref _cancellationTokenSource);

        _onDeactivateEvent.Invoke();
    }

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

    public void Shoot(Vector3 position, Quaternion rotation, Firearm firearm)
    {
        Rigidbody.position = position;
        Rigidbody.rotation = rotation;
        Rigidbody.linearVelocity = Vector3.zero;

        if (firearm.MoveVelocity != default)
            Rigidbody.AddForce(firearm.MoveVelocity, ForceMode.VelocityChange);

        CTSUtility.Reset(ref _cancellationTokenSource);

        _trajectory.Initialize(Rigidbody);

        if (_trajectory is ITrajectoryAsync trajectoryAsync)
            trajectoryAsync.UpdateAsync(Rigidbody, _cancellationTokenSource.Token).Forget();

        _lifeTime.UpdateAsync(_cancellationTokenSource.Token, this, firearm);

        _onShootEvent.Invoke();
    }

    private void OnDisable()
    {
        CTSUtility.Clear(ref _cancellationTokenSource);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_cancellationTokenSource == null)
            return;

        other.TryGetComponent(out IHitbox hitbox);

        // Use hitbox parent if it exist.
        GameObject target = hitbox == null ? other.gameObject : hitbox.Parent;

        if (_teamTag != null && target.CompareTag(_teamTag)) // Ignore if target is in the same team.
            return;

        if (target.TryGetComponent(out IEffector effector)) // If effector exist on the object.
            effector.AddEffects(_effects, gameObject);

        _hit.Initialize(this);

        if (_hit is IHitAsync hitAsync)
            hitAsync.UpdateAsync(_cancellationTokenSource.Token, this);
    }
}
