using System;

namespace CookAGeddon.Gameplay.GrapplingHookUtility
{
	/// <summary>
	/// In this state the player swings with the grappling hook.
	/// </summary>
    public class GrapplingHookSwingState : GrapplingHookBaseState
    {
	    public override void EnterState(GrapplingHook grapplingHook) {
		    grapplingHook.SetStartingVelocity();
	    }
    }
}
