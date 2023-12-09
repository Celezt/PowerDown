using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using URand = UnityEngine.Random;
using UDebug = UnityEngine.Debug;

public class AIWanderBehavior : AIMovingBase
{
	[SerializeField] private float maxMoveTime = 2.5f;

	private float moveTime;

	private Vector3 moveDirection;

	#region OLD-SYSTEM
	// public AIWanderBehavior(AIController _controller, float _maxMoveTime, float _minPlayerDistance, float _detectEdgeDistance, float _detectWallDistance, Transform _player)
	// {
	// 	controller = _controller;
	// 	maxMoveTime = _maxMoveTime;
	// 	minPlayerDistance = _minPlayerDistance;
	// 	player = _player;
	// 	detectEdgeDistance = _detectEdgeDistance;
	// 	detectWallDistance = _detectWallDistance;
	// }
	#endregion

	public override void OnInit() { base.OnInit(); }

	public override void OnEnter() { }

	public override void OnExit()
	{
		controller.Move(Vector2.zero);
	}

	public override void OnUpdate()
	{
		if (Vector3.Distance(player.transform.position, controller.transform.position) <= minPlayerDistance)
		{
			controller.SwitchBehavior(AIState.attacking);
			return;
		}

		if (moveTime <= 0.0f)
		{
			moveDirection = URand.insideUnitSphere;
			moveTime = maxMoveTime;
		}
		else
		{
			moveTime -= Time.deltaTime;
		}

		if (!Physics.Raycast(controller.transform.position + Vector3.up, moveDirection, detectEdgeDistance))
		{
			if (!Physics.Raycast(controller.transform.position + Vector3.up + moveDirection * detectEdgeDistance, Vector3.down, 2))
			{
				controller.Move(Vector2.zero);
				controller.StopMoveAnimation();
				return;
			}
		}

		if (Physics.Raycast(controller.transform.position + Vector3.up, moveDirection, detectWallDistance))
		{
			controller.Move(Vector2.zero);
			moveTime = 0.0f;
			controller.StopMoveAnimation();
			return;
		}

		controller.SetMoveAnimationSpeed(moveAnimSpeed);
		controller.Move(new Vector2(moveDirection.x, moveDirection.z));
		controller.Look(new Vector2(moveDirection.x, moveDirection.z));
	}

	public override void OnFixedUpdate() { }

	public override void OnCollisionEnter(Collision other) { }

	public override void OnCollisionExit(Collision other) { }

	public override void OnTriggerEnter(Collider other) { }

	public override void OnTriggerExit(Collider other) { }
}