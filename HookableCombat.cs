using System;
using CookAGeddon.Gameplay.Combat;
using CookAGeddon.Utility;
using JetBrains.Annotations;
using UnityEngine;

namespace CookAGeddon.Gameplay.Hookables {
	[RequireComponent(typeof(SphereCollider))]
	[RequireComponent(typeof(DelayedDespawn))]
	[RequireComponent(typeof(DamageOnContact))]
	[RequireComponent(typeof(MoveTowards))]
	public class HookableCombat : HookableBase {
		private DelayedDespawn  _delayedDespawn;
		private DamageOnContact _damageOnContact;
		private MoveTowards     _moveTowards;
		private SphereCollider  _damageCollider;

		private void Awake() {
			_delayedDespawn         = GetComponent<DelayedDespawn>();
			_damageOnContact        = GetComponent<DamageOnContact>();
			_moveTowards            = GetComponent<MoveTowards>();
			_damageCollider         = GetComponent<SphereCollider>();
			_damageCollider.enabled = false;
		}

		protected override void DestroyHookable() {
			if (_delayedDespawn)
				_delayedDespawn.Activate();
			base.DestroyHookable();
		}

		public override void Throw(Transform targetPosition, Vector3? direction, float velocity) {
			if (!_canThrow) return;
			_damageCollider.enabled = true;
			if (targetPosition) {
				_moveTowards.SetTarget(() => targetPosition.position, velocity);
				DestroyHookable();
			} else if (direction.HasValue) {
				_moveTowards.SetTarget(() => {
					Vector3 newPosition = transform.position + (direction.Value.normalized * (velocity * Time.fixedDeltaTime));
					return newPosition;
				}, velocity);
				DestroyHookable();
			}
		}

		public override void MoveTo(Vector3 targetPosition, float moveTime) {
			DetachFromParent();

			if (!Rigidbody.isKinematic) Rigidbody.isKinematic = true;

			NextPosition = targetPosition;
			float distanceToPosition = Vector3.Distance(NextPosition.Value, transform.position);
			MoveToVelocity = distanceToPosition / moveTime;
			IsMoving       = true;
		}

		public void SwitchDamageOnContactModeTo(DamageOnContact.OnContact newContactMode) {
			if (_damageOnContact) {
				_damageOnContact.ContactMode = newContactMode;
			}
		}

		public void SetDamageOnContactDamage(float from, float to) {
			if (_damageOnContact) {
				_damageOnContact.Damage = new FloatInterval(from, to);
			}
		}
	}
}
