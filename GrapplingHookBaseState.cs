using System;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	
	/// <summary>
	/// This represents any state the grappling hook could be in.
	/// </summary>
	public interface IGrapplingHookState {
		public void EnterState(GrapplingHook  grapplingHook);
		public void UpdateState(GrapplingHook grapplingHook);
		public void ExitState(GrapplingHook   grapplingHook);
	    
	}
	/// <summary>
	/// Implements the IGrapplingHookState interface, and provides optional methods. 
	/// </summary>
	public abstract class GrapplingHookBaseState : IGrapplingHookState {
		
		/// <summary>
		/// Is called when a state is entered.
		/// </summary>
		/// <param name="grapplingHook"></param>
		public virtual void EnterState(GrapplingHook grapplingHook) {
		}
		
		/// <summary>
		/// Is called while the grappling hook is active, and this is the current state.
		/// </summary>
		/// <param name="grapplingHook"></param>
		public virtual void UpdateState(GrapplingHook grapplingHook) {
		}
		
		/// <summary>
		/// Is called on exiting a state.
		/// </summary>
		/// <param name="grapplingHook"></param>
		public virtual void ExitState(GrapplingHook grapplingHook) {
		}
	}
}
