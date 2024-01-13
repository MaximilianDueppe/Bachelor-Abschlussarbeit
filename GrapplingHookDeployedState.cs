using System;
using UnityEngine;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	/// <summary>
	/// State in which the grappling hook is deployed.
	/// </summary>
	public class GrapplingHookDeployedState : GrapplingHookBaseState {
		public override void EnterState(GrapplingHook grapplingHook) {
			grapplingHook.InitialPlacement();
			if(!grapplingHook.Deploy()) grapplingHook.ResetHook();
		}

		public override void UpdateState(GrapplingHook grapplingHook) {
			// Grappling Hook reached max distance without hitting the target
			if (grapplingHook.ReachedMaxDistance()) {
				grapplingHook.WasAttachedBefore = false;
				grapplingHook.SwitchState(GrapplingHook.States.Retract);
				return;
			}

			if (grapplingHook.HitTarget) {
				if (!grapplingHook.HitCorrectTarget()) { // Grappling Hook did not hit correct target
					grapplingHook.WasAttachedBefore = false;
					grapplingHook.SwitchState(GrapplingHook.States.Retract);
				} else { // Grappling Hook hit correct target
					grapplingHook.SwitchState(GrapplingHook.States.Attach);
				}
			}
		}

		}
}
