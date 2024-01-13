using System;
using CookAGeddon.Gameplay.GrapplingHookUtility;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using NaughtyAttributes;
using UnityEngine;

namespace CookAGeddon.Gameplay.Abilities {
	/// <summary>
	/// Base ability for all grappling hook abilities.
	/// Will be obsolete later, when final game design decisions were made.
	/// Then only one ability will be used and all this code will be moved there.
	/// </summary>
	public abstract class GHBaseAbility : AbilityBase {
		/// <summary>
		/// Setting of the specific grappling hook ability
		/// </summary>
		[Expandable] [SerializeField] protected                                               GrapplingHookSetting _setting;
		
		/// <summary>
		/// The grappling hook that is used in game.
		/// </summary>
		[SerializeField]              protected                                               GrapplingHook        _grapplingHook;
		
		/// <summary>
		/// Every ability has a cooldown, which can be set here.
		/// </summary>
		[SerializeField]              protected                                               float                _cooldown;
		
		/// <summary>
		/// The current cooldown. 
		/// </summary>
		protected                                                                             float                CurrentCooldown;
		
		/// <summary>
		/// The player representation.
		/// </summary>
		protected HeroMovement         Player;
		
		/// <summary>
		/// Will later be deleted, when custom animations for the grappling hook have been created.
		/// </summary>
		[Foldout(                                          "Dev")] [SerializeField] protected float                _animationTimeOffset;

		protected void OnEnable() {
			if (!Player) Player = GetComponent<HeroMovement>();
			_grapplingHook.Setting = _setting;
		}
		
	}
}
