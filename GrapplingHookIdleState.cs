using System;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	/// <summary>
	/// State in which the grappling hook is waiting for activation.
	/// </summary>
	public class GrapplingHookIdleState : GrapplingHookBaseState{

		public override void UpdateState(GrapplingHook grapplingHook) {
			// if path is not clear break execution
			if (!grapplingHook.IsPathClear()) {
				grapplingHook.ResetHook();
			}
			
			// if enemy is not far enough away
			if (!grapplingHook.HasMinDistance()) {
				grapplingHook.ResetHook();
			}


			grapplingHook.SwitchState(GrapplingHook.States.Deploy);
		
		}
	}
}
