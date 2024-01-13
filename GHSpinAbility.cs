using System;
using System.Collections;
using CookAGeddon.Gameplay.Combat;
using CookAGeddon.Gameplay.GrapplingHookUtility;
using CookAGeddon.Gameplay.Hookables;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using EasyCharacterMovement;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;


namespace CookAGeddon.Gameplay.Abilities {
	public class GHSpinAbility : GHBaseAbility {
		#region serialized fields

		[Tooltip("How long the spins should take. Performs the number of spins specified in this amount of time.")]
		[SerializeField]
		private float _spinTime;

		[Tooltip("How many spins should be performed")]
		[SerializeField]
		private float _spins;

		[Tooltip("If rotation should be counter clockwise")]
		[SerializeField]
		private bool _rotateCounterClockwise = false;

		[Tooltip("The transform which will rotate around the player. Hookable is attached to it as a child.")]
		[SerializeField]
		private Transform _spinTarget;

		[Tooltip("If the distance of the hookable should change while spinning.")]
		[SerializeField]
		private bool _changeDistance = false;

		#endregion

		#region private properties & fields

		#region fields
		
		/// <summary>
		/// Where the rotation should start.
		/// </summary>
		private Vector3        _rotationStartPosition;
		private Coroutine      _spinCoroutine;
		
		private HookableCombat _target;
		
		/// <summary>
		/// The tangent of the circle the spinning describes.
		/// </summary>
		private Vector3 _tangent;
		
		/// <summary>
		/// The current time the hookable was spun.
		/// </summary>
		private float          _spinOverTime;

		#endregion

		#endregion

		
		public override bool Activate() {
			_target = AimingController.Instance.TargetHook as HookableCombat;
			
			if (!_target) return false;
			if (!_target.IsHookable) return false;
			if (_spinCoroutine != null) return false;
			if (!_grapplingHook.IsIdle) return false;

			_grapplingHook.SetTarget(_target);
			_grapplingHook.Setting = _setting;
			var distance = Vector3.Distance(Player.GetPosition(), _target.AnchorPosition);
			_spinCoroutine = StartCoroutine(SpinCoroutine(distance));
			return true;

		}

		
		private IEnumerator SpinCoroutine(float distance) {
			StartAnimation();
			yield return new WaitForSecondsRealtime(AnimationTime + _animationTimeOffset);
			StopAnimation();

			_grapplingHook.ToggleGrapplingHook();

			while (_grapplingHook.IsIdle) yield return null;
			while (_grapplingHook.IsDeployed) yield return null;
			
			if (!_grapplingHook.IsAttached) {
				Player.Unblock();
				Finish();
				yield break;
			}

			StartAnimation(1);
			yield return new WaitForSecondsRealtime(0.05f);
			int sign     = _rotateCounterClockwise ? -1 : 1;
			var maxAngle = _spins * 360;
			var angle    = (maxAngle / _spinTime) * sign;

			var endDistance = distance;
			Player.SetRotationMode(RotationMode.Custom);

			// Setup spinTarget and move hookable to it
			Vector3 newSpinTargetPosition = Player.GetPosition() +
			                                (_target.AnchorPosition - Player.GetPosition()).normalized * _grapplingHook.Setting.MinTravelDistance +
			                                Vector3.up;
			distance                = _grapplingHook.Setting.MinTravelDistance;
			newSpinTargetPosition.y = (Player.GetPosition() + Vector3.up * Player.GetHeight() * 0.5f).y;
			_spinTarget.position    = newSpinTargetPosition;
			_rotationStartPosition  = _spinTarget.position;

			_target.MoveTo(_rotationStartPosition, 0.05f);

			while (_target.ForcedMovement) {
				yield return null;
			}

			_target.SwitchLayerTo("Player");
			_target.AttachToParent(_spinTarget, true);
			Player.Block();


			var distanceStep = (_grapplingHook.Setting.MaxTravelDistance - distance) / _spinTime;
			
			// start spinning
			_spinOverTime = Time.time + _spinTime;
			while (_grapplingHook.IsAttached && _spinOverTime > Time.time) {
				Player.LookAtPosition = _spinTarget.position;
				if (distance < endDistance && _changeDistance) distance += distanceStep * Time.unscaledDeltaTime;
				var direction                                           = (_spinTarget.position - (Player.GetPosition())).normalized;
				_spinTarget.position = Player.GetPosition() + direction * distance;
				_spinTarget.RotateAround(Player.GetPosition(), Player.transform.up, angle * Time.unscaledDeltaTime);
				Vector3 toPlayer = Player.GetPosition() - _spinTarget.position;
				_tangent = Vector3.Cross(toPlayer, Vector3.up);
				_target.OrientToTarget(_tangent);

				yield return null;
			}

			Player.Unblock();
			StopAnimation();

			Finish();
		}

		public override bool Finish() {
			ThrowObject();
			_spinOverTime = Time.time;
			if(CurrentCooldown <= Time.time)
				CurrentCooldown = Time.time + _cooldown;
			
			StopAnimation();
			if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
			_spinCoroutine = null;
			
			if (Player.IsBlocked()) Player.Unblock();
			return true;
		}

		private void ThrowObject() {
			if (_target) {
				_target.DetachFromParent();
				_target.SwitchDamageOnContactModeTo(DamageOnContact.OnContact.Destroy);
				_target.SetDamageOnContactDamage(5f, 25f);
				var throwTarget = AimingController.Instance.TargetAttack;
				if (throwTarget)
					_target.Throw(throwTarget.transform, null, _grapplingHook.Setting.ThrowSpeed);
				else {
					_target.Throw(null, _tangent, _grapplingHook.Setting.ThrowSpeed);
				}
			}

			if (_grapplingHook.IsAttached) _grapplingHook.DelayedDetach(0.5f);
		}

		public override void Cancel() {
			_grapplingHook.ResetHook();
		}

		private void OnDrawGizmos() {
			Gizmos.color = Color.red;
			if (!_spinTarget) return;
			Gizmos.DrawWireSphere(_spinTarget.position, 0.4f);
		}
	}
}
