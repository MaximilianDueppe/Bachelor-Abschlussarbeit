using CookAGeddon.Utility;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	[CreateAssetMenu(fileName = "Grappling Hook Setting", menuName = "Cook-A-Geddon/Data/Grappling Hook Setting", order = 1)]
	public class GrapplingHookSetting : ScriptableObject {
		[Tooltip("The speed of the grappling hook at deployment.")]
		[Range(25f, 250f)]
		public float DeploySpeed;

		[Tooltip("The speed of the grappling hook when retracting.")]
		[Range(25f, 250f)]
		public float RetractSpeed;

		[Tooltip("The strength with which the grappling hook pulls on objects, or the player. Can be interpreted for various situations.")]
		[Range(1f, 50f)]
		public float PullStrength;

		[Range(1f, 50f)] public float ThrowSpeed;
		[Range(1f, 15f)] public float MaxRopeLength;

		[Tooltip("The minimum distance the grappling hook must travel.")]
		[Range(1f, 50f)]
		public float MinTravelDistance;

		[Tooltip("The maximum distance the grappling hook can travel.")]
		[Range(1f, 50f)]
		public float MaxTravelDistance;

		[Tooltip("If the retraction of the hook should be animated.")]
		public bool AnimateRetracting;

		[Tooltip("If the MaxRopeLength should be ignored.")]
		public bool UseCurrentDistance;

		[Tooltip("Layers used to determine if there is something in the path of the grappling hook or the player.")]
		public LayerMask ObstacleLayerMask;
	}
}
