using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public enum AIState
{
	wandering,
	attacking,
	undefined
}

public class AIController : MonoBehaviour
{
	[Header("AI Update Method"), Tooltip("Set to false to use Unity's update method. Set to true to update AI via the AIManager's update event.")]
	[SerializeField] bool useManagerUpdate = false;
	[Header("Generic AI settings")]
	[SerializeField] TargetBehaviour lookController;
	[SerializeField] MoveBehaviour moveController;
	[SerializeField] AIMovingBase wanderingBehavior;
	[SerializeField] AIAttackingBase attackingBehavior;

	public AIState activeState { get; private set; } = AIState.undefined;

	public delegate void OnAIDestroyedEvent(AIController controller);
	public event OnAIDestroyedEvent OnAIDestroyed;

	private AIBaseBehavior activeBehavior;

	public DungeonRoom room { get; set; }

	private bool bWasDestroyed = false;

	// Start is called before the first frame update
	void Start()
	{
		if (useManagerUpdate && AIManager.INSTANCE)
			AIManager.INSTANCE.Subscribe(this);

		if (!wanderingBehavior)
			wanderingBehavior = GetComponent<AIMovingBase>();

		if (!attackingBehavior)
			attackingBehavior = GetComponent<AIAttackingBase>();

		SwitchBehavior(AIState.wandering);
	}

	// Update is called once per frame
	void Update()
	{
		if (AIManager.INSTANCE && useManagerUpdate || bWasDestroyed)
			return;
		activeBehavior?.OnUpdate();
	}

	public void UpdateAI()
	{
		activeBehavior?.OnUpdate();
	}

	public void NotifyAIKilled()
	{
		OnAIDestroyed?.Invoke(this);
		activeBehavior?.OnExit();
		if (!AIManager.INSTANCE || !useManagerUpdate)
		{
			bWasDestroyed = true;
			return;
		}
		AIManager.INSTANCE.UnSubscribe(this);
	}

	public void NotifyAIWasAttacked() => SwitchBehavior(AIState.attacking);

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		activeBehavior?.OnDrawGizmos();
	}
#endif

	void OnDestroy()
	{
		NotifyAIKilled();
	}

	void OnDisable()
	{
		NotifyAIKilled();
	}

	public void SwitchBehavior(AIState newState)
	{
		if (newState == activeState)
			return;

		activeBehavior?.OnExit();
		switch (newState)
		{
		case AIState.wandering:
			activeBehavior = wanderingBehavior;
			break;
		case AIState.attacking:
			activeBehavior = attackingBehavior;
			break;
		default:
			break;
		}
		activeBehavior?.OnInit();
		activeBehavior?.OnEnter();
		activeState = newState;
	}

	public void Move(Vector2 direction) => moveController.Move(direction);
	public Vector3 GetMoveDirection() => moveController.Direction;
	public bool GetIsMoving() => moveController.IsMoving;
	public Quaternion GetMoveLookRotation() => moveController.LookRotation;
	public float GetMoveForce() => moveController.MoveForce;

	public void LookAt(Vector2 target) => lookController.LookAt(target);
	public void Look(Vector2 direction) => lookController.Look(direction);
}