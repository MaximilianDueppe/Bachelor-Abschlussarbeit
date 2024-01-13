using CookAGeddon.Utility;
using UnityEngine;

namespace CookAGeddon.Gameplay.Hookables {
	public class HookableItem : HookableCombat {
		public override bool Pull(Vector3? target, float force) {
			if (_canPull) {
				DetachFromParent();

				Vector3 pullDirection;
				if (target.HasValue) {
					pullDirection = target.Value - transform.position;
				} else {
					pullDirection = MathUtils.RandomVector3OnUnitCircle();
				}


				// if you pull any object apply a small up force to prevent colliding with ground
				if (!Rigidbody.isKinematic) {
					// object has been made kinematic once
					Rigidbody.isKinematic = true;
					Rigidbody.position    = (Rigidbody.position + (0.01f * Vector3.up));
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
	}
}
