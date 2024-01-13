using CookAGeddon.Gameplay.Combat;
using CookAGeddon.Utility;
using UnityEngine;

namespace CookAGeddon.Gameplay.Hookables {
	public class HookableEquipment : HookableCombat, IDamageFilter {
		private void Start() {
			var destructible = GetComponentInParent<Destructible>();

			if (!destructible) return;

			destructible.AddDamageFilter(this);
		}
		
		

		public DamageParams FilteredDamage(GameObject target, DamageParams damage) {
			if (!damage.Source || damage.Value == 0) return damage;

			damage.Value = 0f;
			return damage;
		}

		public override bool Pull(Vector3? target, float force) {
			if (_canPull) {
				var destructible = GetComponentInParent<Destructible>();

				if (destructible)
					destructible.RemoveDamageFilter(this);

				DetachFromParent();

				Vector3 pullDirection;
				if (target.HasValue) {
					pullDirection = target.Value - transform.position;
				} else {
					pullDirection = MathUtils.RandomVector3OnUnitCircle();
				}
				
				
				// if you pull any object apply a small up force to prevent colliding with ground
				if (!Rigidbody.isKinematic) { // object has been made kinematic once
					Rigidbody.isKinematic = true;
					Rigidbody.position = (Rigidbody.position + (0.01f * Vector3.up));
				}
				
				Rigidbody.isKinematic = false;
				Rigidbody.AddForce((pullDirection.normalized + (0.01f * Vector3.up)) * force, ForceMode.VelocityChange);
				
				if (_destroyAfterUse) {
					DestroyHookable();
				} else {
					ToggleHookable(_usableAfterPull);
				}
				
				return true;
			}

			return false;
		}

		public override void MoveTo(Vector3 targetPosition, float moveTime) {
			var destructible = GetComponentInParent<Destructible>();

			if (destructible)
				destructible.RemoveDamageFilter(this);
			
			base.MoveTo(targetPosition, moveTime);
		}

	
	}
}
