using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[HideMonoScript]
public class Ragdoll : MonoBehaviour
{
    public bool IsRagdoll => _isRagdoll;

    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private bool _activateOnStart = false;

    [SerializeField]
    private bool _resetTransformOnDeactivate = true;
    [SerializeField]
    private bool _teleportOnDeactivate = false;
    [SerializeField, ShowIf(nameof(_teleportOnDeactivate)), Indent]
    private Transform _copyTransformOnTeleport;

    [SerializeField, PropertySpace(SpaceBefore = 4)]
    private Rigidbody _actorRigidbody;

    [SerializeField, PropertySpace(SpaceBefore = 8), DrawWithUnity, DisableInPlayMode]
    private Rigidbody[] _ragdollRigidbodies;

    [SerializeField, PropertySpace(SpaceBefore = 8)]
    private UnityEvent _onRagdollActivateEvent;
    [SerializeField]
    private UnityEvent _onRagdollDeactivateEvent;

    private Vector3[] _initialPositions;
    private Quaternion[] _initialRotation;
    private List<Joint> _joints = new();
    private bool _isRagdoll;
    private bool _isInitialized;
    private CachedPush? _cachedPush;

    private struct CachedPush
    {
        public float Force;
        public Vector3 Position;
        public float Radius;
        public float UpwardModifier;
        public float TimeSnapshot;
    }

    public void Push(float force, Vector3 position, float radius, float upwardsModifier = 1.0f)
    {
        if (!_isRagdoll)    // Cash push if currently not in ragdoll form.
        {
            _cachedPush = new CachedPush
            {
                Force = force,
                Position = position,
                Radius = radius,
                UpwardModifier = upwardsModifier,
                TimeSnapshot = Time.time
            };
        }
        else
        {
            for (int i = 0; i < _ragdollRigidbodies.Length; i++)
            {
                var rigidbody = _ragdollRigidbodies[i];
                rigidbody.AddExplosionForce(force, position, radius, upwardsModifier, ForceMode.Impulse);
            }
        }
    }

    public void OnEnableRagdoll()
    {
        foreach (var rigidbody in _ragdollRigidbodies)
        {
            rigidbody.detectCollisions = true;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.velocity = Vector3.zero;
        }

        foreach (var joint in _joints)
        {
            joint.enableCollision = true;
        }

        _actorRigidbody.detectCollisions = false;
        _actorRigidbody.isKinematic = true;

        if (_animator)
            _animator.enabled = false;

        _isRagdoll = true;

        if (_cachedPush is { } cached)
        {
            if (Time.time - cached.TimeSnapshot > 1f)   // Remove cached if it is older than 1 sec.
                _cachedPush = null;
            else
                Push(cached.Force, cached.Position, cached.Radius, cached.UpwardModifier);
        }

        _onRagdollActivateEvent.Invoke();
    }

    public void OnDisableRagdoll()
    {
        if (_isInitialized && _teleportOnDeactivate && _copyTransformOnTeleport)
        {
            _actorRigidbody.position = _copyTransformOnTeleport.position;
            _actorRigidbody.rotation.SetLookRotation(_copyTransformOnTeleport.forward._y_().normalized);
        }

        for (int i = 0; i < _ragdollRigidbodies.Length; i++)
        {
            var rigidbody = _ragdollRigidbodies[i];
            rigidbody.detectCollisions = false;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            if (_resetTransformOnDeactivate)
            {
                rigidbody.position = _initialPositions[i];
                rigidbody.rotation = _initialRotation[i];
            }
        }

        foreach (var joint in _joints)
        {
            joint.enableCollision = false;
        }

        _actorRigidbody.detectCollisions = true;
        _actorRigidbody.isKinematic = false;

        if (_animator)
            _animator.enabled = true;

        _isRagdoll = false;

        _onRagdollDeactivateEvent.Invoke();
    }

    private void Awake()
    {
        int length = _ragdollRigidbodies.Length;
        _initialPositions = new Vector3[length];
        _initialRotation = new Quaternion[length];

        for (int i = 0; i < length; i++)
        {
            var rigidbody = _ragdollRigidbodies[i];
            var joint = rigidbody.GetComponent<Joint>();
            if (joint != null)
                _joints.Add(joint);

            _initialPositions[i] = rigidbody.position;
            _initialRotation[i] = rigidbody.rotation;
        }
    }

    private void Start()
    {
        if (_activateOnStart)
            OnEnableRagdoll();
        else
            OnDisableRagdoll();

        _isInitialized = true;
    }

#if UNITY_EDITOR
    private const string ACTIVATE_RAGDOLL = "Activate Ragdoll";
    private const string DEACTIVATE_RAGDOLL = "Deactivate Ragdoll";

    [Button("@_isRagdoll ? DEACTIVATE_RAGDOLL : ACTIVATE_RAGDOLL"), PropertyOrder(-1), HideInEditorMode]
    private void SwitchState()
    {
        if (!_isRagdoll)
            OnEnableRagdoll();
        else
            OnDisableRagdoll();
    }
#endif
}
