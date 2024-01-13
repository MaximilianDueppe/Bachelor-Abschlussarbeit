using System;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	/// <summary>
	/// In this state the grappling hook moves back to the player.
	/// </summary>
	public class GrapplingHookRetractState : GrapplingHookBaseState {
		public override void EnterState(GrapplingHook grapplingHook) {
			grapplingHook.Retract();
		}
		
	}
}
