using System.Collections;
using CookAGeddon.Gameplay.Hookables;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using UnityEngine;


namespace CookAGeddon.Gameplay.Abilities {
	public class GHSwingAbility : GHBaseAbility {
		[Tooltip("Set this variable to 'true' to require the player to be airborne for ability activation. Set to 'false' if airborne status is not necessary for activation.")]
		[SerializeField]
		private bool _useAirborneConstraint = true;

		private HookableBase _target;
		private Coroutine    _grapplingHookCoroutine;

		
		public override bool Activate() {
			_target = AimingController.Instance.TargetHook as HookableSwing;

			if (!_target) return false;
			if (!_target.IsHookable) return false;
			if (CurrentCooldown > Time.time) return false;
			if (_grapplingHookCoroutine != null) return false;


			if (!_grapplingHook.IsIdle) return false;

			_grapplingHook.SetTarget(_target);
			_grapplingHook.Setting = _setting;

			bool airborne = !Player.IsOnGround();
			if ((!airborne && !_useAirborneConstraint) || airborne) {
				_grapplingHookCoroutine = StartCoroutine(GrapplingHookCoroutine());
				return true;
			}

			return false;
		}


		private IEnumerator GrapplingHookCoroutine() {
			StartAnimation();
			yield return new WaitForSecondsRealtime(AnimationTime + _animationTimeOffset);
			StopAnimation();

			Player.Block();

			_grapplingHook.ToggleGrapplingHook();

			while (_grapplingHook.IsIdle) {
				yield return null;
			}

			yield return new WaitForSecondsRealtime(0.05f);

			while (_grapplingHook.IsDeployed) {
				yield return null;
			}

			yield return new WaitForSecondsRealtime(0.05f);

			if (!_grapplingHook.IsAttached) {
				Player.Unblock();
				Finish();
				yield break;
			}


			Player.Unblock();

			yield return new WaitForSecondsRealtime(0.05f);

			_grapplingHook.StartSwinging();
			while (_grapplingHook.IsSwinging) {
				yield return null;
			}

			Finish();
		}

		public override bool Finish() {
			if (CurrentCooldown <= Time.time)
				CurrentCooldown = Time.time + _cooldown;
			StopAnimation();
			if (_grapplingHookCoroutine != null) StopCoroutine(_grapplingHookCoroutine);

			_grapplingHookCoroutine = null;
			_grapplingHook.StopSwinging();


			return true;
		}


		public override void Cancel() {
			_grapplingHook.ResetHook();
		}
	}
}
