using System;
using CookAGeddon.Gameplay.Hookables;
using UnityEngine;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	public class HookableToPoint : HookableBase {
		[Tooltip("How far up the target point should be.")]
		[SerializeField] private float          _heightOffset;
		[Tooltip("How far away from the ledge the target point should be.")]
		[SerializeField] private float          _ledgeOffset;
		
		[Tooltip("Used to calculate the waypoint the player should move through.")]
		[SerializeField] private AnimationCurve _curve    = AnimationCurve.Constant(0, 1.0f, 1.0f);
		[Tooltip("How many waypoints should be calculated.")]
		[SerializeField] private int            _accuracy = 25;
		private                  Vector3        TargetPoint => RelativeTargetPoint();
		private Vector3 RelativeTargetPoint() {
			Vector3 targetPosition = AnchorPosition;
			Vector3 position       = transform.position;

			float yDifference = position.y - targetPosition.y;

			targetPosition.y += _heightOffset + yDifference;

			Vector3 direction = targetPosition - Player.GetPosition();
			direction.y = 0f;


			Vector3 relativeTargetPoint = targetPosition + direction.normalized * _ledgeOffset;
			return relativeTargetPoint;
		}

		public Vector3[] CalcWaypoints() {
			Vector3   targetPosition  = TargetPoint;
			Vector3   playerPosition  = Player.GetPosition();
			
			Vector3[] returnWaypoints = new Vector3[_accuracy];
			float     fullDistance    = Vector3.Distance(playerPosition, targetPosition);
			Vector3   moveDirection   = (targetPosition - playerPosition).normalized;
			Vector3   startPosition   = playerPosition;

			for (int i = 1; i < _accuracy; i++) {
				float traveledDistance = i / (float) _accuracy;
				float curveHeight      = _curve.Evaluate(traveledDistance);
				returnWaypoints[i - 1]   =  startPosition + moveDirection * (fullDistance * traveledDistance);
				returnWaypoints[i - 1].y *= curveHeight;
			}

			returnWaypoints[_accuracy - 1] = targetPosition;
			return returnWaypoints;
		}

		protected override void OnDrawGizmos() {
			base.OnDrawGizmos();
			if (Player) {
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(TargetPoint, 0.2f);

				Gizmos.color = Color.black;
				Vector3 targetPosition = TargetPoint;
				Vector3 playerPosition = Player.GetPosition();
				Vector3 moveDirection  = (targetPosition - playerPosition);

				Gizmos.DrawLine(playerPosition, playerPosition + moveDirection);
		
				Gizmos.color = Color.red;
				var waypoints = CalcWaypoints();
				for (int i = 0; i < waypoints.Length - 1; i++) {
					Gizmos.DrawSphere(waypoints[i], 0.1f);
					Gizmos.DrawLine(waypoints[i], waypoints[i+1]);
				}
			}
		}
	}
}
