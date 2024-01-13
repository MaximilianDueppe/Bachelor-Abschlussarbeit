using System.Collections;
using CookAGeddon.Gameplay.GrapplingHookUtility;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace CookAGeddon.Gameplay.Abilities {
	public class GHToPointAbility : GHBaseAbility {
		
		private Coroutine       _grappleToPointCoroutine;
		private HookableToPoint _target;
		
		private Vector3[] _waypoints;
		
		public override bool Activate() {
			_target = AimingController.Instance.TargetHook as HookableToPoint;

			if (!_target) return false;
			if (_target.AnchorPosition.y < Player.GetPosition().y) return false;
			if (_grappleToPointCoroutine != null) return false;


			if (!_grapplingHook.IsIdle) return false;
			_grapplingHook.SetTarget(_target);
			_grapplingHook.Setting   = _setting;
			_waypoints               = _target.CalcWaypoints();
			_grappleToPointCoroutine = StartCoroutine(GrappleToPointCoroutine());
			return true;
		}

		private IEnumerator GrappleToPointCoroutine() {
			StartAnimation();
			yield return new WaitForSecondsRealtime(AnimationTime + _animationTimeOffset);
			StopAnimation();

			
			_grapplingHook.ToggleGrapplingHook();
			
			while (_grapplingHook.IsIdle) {
				yield return null;
			}
			yield return new WaitForSecondsRealtime(0.05f);

			while (_grapplingHook.IsDeployed) {
				yield return null;
			}

			if (!_grapplingHook.IsAttached) {
				Player.Unblock();
				Finish();
				yield break;
			}
			
			Player.Block();
			
			_grapplingHook.MovePlayerThroughWaypoints(_waypoints);	
			
			Finish();
			
		}

		public override bool Finish() {
			if(CurrentCooldown <= Time.time)
				CurrentCooldown = Time.time + _cooldown;
			StopAnimation();

			_grappleToPointCoroutine = null;
			
			return true;
		}
		public override void Cancel() {
			_grapplingHook.ResetHook();
		}
	}
}
