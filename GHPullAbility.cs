using System.Collections;
using CookAGeddon.Gameplay.Hookables;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using UnityEngine;


namespace CookAGeddon.Gameplay.Abilities {
	public class GHPullAbility : GHBaseAbility {
		#region serialized fields

		/// <summary>
		/// Represents the position to where any target is pulled.
		/// </summary>
		[SerializeField]
		private Transform _pullTarget;

		/// <summary>
		/// How long detach should be delayed.
		/// </summary>
		[SerializeField]
		private float _detachDelay;

		#endregion

		#region private properties & fields

		#region fields

		/// <summary>
		/// The target of the grappling hook.
		/// </summary>
		private HookableBase _target;

		/// <summary>
		/// The coroutine which executes the ability.
		/// </summary>
		private Coroutine _pullCoroutine;

		#endregion

		#endregion
		
		public override bool Activate() {
			_target = AimingController.Instance.TargetHook as HookableCombat;
			if (!_target) return false;


			if (CurrentCooldown > Time.time) return false;

			if (_pullCoroutine != null) return false;

			if (!_target.IsHookable) return false;

			if (!_grapplingHook.IsIdle) return false;

			_grapplingHook.SetTarget(_target);
			_grapplingHook.Setting = _setting;

			_pullCoroutine = StartCoroutine(PullCoroutine());

			return true;
		}


		private IEnumerator PullCoroutine() {
			StartAnimation();

			yield return new WaitForSecondsRealtime(AnimationTime + _animationTimeOffset);

			StopAnimation();

			Player.Block();

			_grapplingHook.ToggleGrapplingHook();

			while (_grapplingHook.IsIdle) {
				yield return null;
			}

			yield return new WaitForSecondsRealtime(0.05f);

			TimeScaleManagerGlobal.Instance.RequestTimeScale(0.5f, 0.2f);
			
			while (_grapplingHook.IsDeployed) {
				ActorEnemy enemy = _target.gameObject.GetComponentInParent<ActorEnemy>();
				if (enemy) enemy.StaggerEnemy();
				yield return null;
			}

			if (!_grapplingHook.IsAttached) {
				Player.Unblock();
				Finish();
				yield break;
			}

			if (_grapplingHook.IsAttached) {
				yield return new WaitForSecondsRealtime(0.15f);
				_target.Pull(_pullTarget.position, _setting.PullStrength);
				_grapplingHook.DelayedDetach(_detachDelay);
			}

			_pullCoroutine  = null;
			CurrentCooldown = Time.time + _cooldown;
			Player.Unblock();
		}
		

		public override void Cancel() {
			_grapplingHook.ResetHook();
		}
	}
}
