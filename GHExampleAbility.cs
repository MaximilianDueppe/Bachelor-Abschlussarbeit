using System.Collections;
using CookAGeddon.Gameplay.Hookables;
using CookAGeddon.Gameplay.Player;
using UnityEngine;

namespace CookAGeddon.Gameplay.Abilities {
	/// <summary>
	/// This is an example for a grappling hook ability. 
	/// </summary>
	public class GHExampleAbility : GHBaseAbility {
		private HookableBase _target;
		private Coroutine    _abilityCoroutine;

		public override bool Activate() {
			_target = AimingController.Instance.TargetHook as HookableBase;

			if (!_target) return false;
			if (!_target.IsHookable) return false;
			if (!_grapplingHook.IsIdle) return false;

			// set the new target
			_grapplingHook.SetTarget(_target);
			_grapplingHook.Setting = _setting;
			_abilityCoroutine      = StartCoroutine(AbilityCoroutine());
			return true;
		}

		private IEnumerator AbilityCoroutine() {
			StartAnimation();
			yield return new WaitForSecondsRealtime(0.5f);
			StopAnimation();

			// activate grappling hook
			_grapplingHook.ToggleGrapplingHook();

			// wait for idle state to be exited
			while (_grapplingHook.IsIdle) yield return null;

			// do stuff after idle state has been exited or while deployed state is active
			while (_grapplingHook.IsDeployed) yield return null;

			// react on state after deployed state has been exited
			if (_grapplingHook.IsRetracting) Player.Block();

			// ... 
		}

		public override void Cancel() {
			StopCoroutine(_abilityCoroutine);
			_abilityCoroutine = null;
		}
	}
}
