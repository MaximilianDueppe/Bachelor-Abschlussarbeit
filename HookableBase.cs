using System;
using CookAGeddon.Gameplay.Combat;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using EasyCharacterMovement;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookAGeddon.Gameplay.Hookables {
	public interface IHookable : ITargetable {
		bool Pull(Vector3?  target,         float force);
		void MoveTo(Vector3 targetPosition, float moveTime);

		void Throw([CanBeNull] Transform targetPosition, Vector3? toTarget, float velocity);

		void SetActiveTarget(bool isActive = true);
	}

	[RequireComponent(typeof(Rigidbody))]
	public abstract class HookableBase : MonoBehaviour, IHookable {
		#region Serialized Fields

		[Tooltip("If the Hookable can be thrown.")]
		[Foldout("Hookable Options")]
		[SerializeField]
		protected bool _canThrow = false;

		[Tooltip("If the Hookable can be pulled.")]
		[Foldout("Hookable Options")]
		[SerializeField]
		protected bool _canPull = false;

		[Tooltip("If the Hookable is still usable after it has been pulled.")]
		[Foldout("Hookable Options")]
		[SerializeField]
		protected bool _usableAfterPull = false;

		[Tooltip("If the Hookable should be destroyed after use.")]
		[Foldout("Hookable Options")]
		[SerializeField]
		protected bool _destroyAfterUse = false;

		[Tooltip("If the anchor is relative to the player position.")]
		[Foldout("Target Options")]
		[SerializeField]
		protected bool _anchorRelativeToPlayer;

		[Tooltip("If the anchor position should be where the anchor transform is.")]
		[Foldout("Target Options")]
		[SerializeField]
		protected bool _useAnchorPosition;

		[Foldout("Target Options")]
		[SerializeField]
		private Transform _anchor;

		[Foldout("Aiming System")]
		[SerializeField]
		private int _targetPriority = 50;

		#endregion

		#region public properties & fields

		#region properties

		#region ITargetable
		
		public bool Usable => _isHookable;

		public int TargetPriority {
			set => _targetPriority = value;
			get => _targetPriority;
		}

		public Vector3 IdealTargetPosition => AnchorPosition;

		#endregion

		/// <summary>
		/// If the anchor position is relative to the player.
		/// </summary>
		public bool IsAnchorRelative => _anchorRelativeToPlayer;

		/// <summary>
		/// If the hookable is currently moving. 
		/// </summary>
		public bool    ForcedMovement => IsMoving;
		
		/// <summary>
		/// If the hookable is usable for the grappling hook.
		/// </summary>
		public bool    IsHookable     => _isHookable;
		
		public Vector3 AnchorPosition => _anchorRelativeToPlayer ? RelativePosition() : _useAnchorPosition ? _anchor.position : transform.position;

		#endregion

		#endregion

		#region protected properties & fields

		#region properties

		/// <summary>
		/// If this hookable is the current active target, determined by the AimingController.
		/// </summary>
		protected bool IsActiveTarget { get; private set; } = false;

		#endregion

		#region fields

		/// <summary>
		/// The attached Rigidbody.
		/// </summary>
		protected Rigidbody Rigidbody;

		/// <summary>
		/// If the hookable is moving right now.
		/// </summary>
		protected bool IsMoving = false;

		/// <summary>
		/// When MoveTo is called, this is the position the hookable should reach.
		/// </summary>
		protected Vector3? NextPosition;

		/// <summary>
		/// Velocity for MoveTo calls.
		/// </summary>
		protected float MoveToVelocity;

		/// <summary>
		/// Used for determining player position, rotation, etc.
		/// </summary>
		protected HeroMovement Player;

		#endregion

		#endregion

		#region private properties & fields

		#region fields
		
		private Transform _lastParent;

		private bool _isHookable = true;

		#endregion

		#endregion

		#region unity lifecylce methods

		private void OnValidate() {
			Rigidbody = GetComponent<Rigidbody>();
		}


		private void Start() {
			Player = HeroMovementReferenceGetter.Instance.GetPlayer(HeroSlot.Italian);
		}

		private void FixedUpdate() {
			if (IsMoving) {
				if (NextPosition.HasValue) {
					float distanceToPosition = Vector3.Distance(NextPosition.Value, transform.position);
					if (Mathf.Approximately(distanceToPosition, 0f)) {
						Rigidbody.MovePosition(Vector3.MoveTowards(Rigidbody.position, NextPosition.Value, MoveToVelocity * Time.fixedDeltaTime));
						Rigidbody.MoveRotation(Quaternion.LookRotation(NextPosition.Value - Rigidbody.position, transform.up));
					} else {
						IsMoving = false;
					}
				}
			}
		}

		#endregion


		#region IHookable methods

		/// <summary>
		/// Override this for hookables that can be used wit non kinematic rigidbodies. 
		/// </summary>
		/// <param name="target">The direction in which the Hookable should be moved. Can be null.</param>
		/// <param name="force">The amount of force that should be applied to the rigidbody.</param>
		/// <returns><b>True: </b> if the hookable can be pulled.<br></br> <b>False:</b> if the hookable should not be pulled, e.g. hookable is heavier than player.</returns>
		public virtual bool Pull(Vector3? target, float force) { return true; }

		/// <summary>
		/// Pulls kinematic Hookables to a specified target position. Always moves the Hookable to the target position in specified time. Use if
		/// the Hookable needs to move to a specific position before doing something else with it and movement is time critical. 
		/// </summary>
		/// <param name="targetPosition">Position where Hookable should end up.</param>
		/// <param name="moveTime">Time in which it should move to target position.</param>
		public virtual void MoveTo(Vector3 targetPosition, float moveTime) { }

		public virtual void Throw([CanBeNull] Transform targetPosition, Vector3? toTarget, float velocity) { }

		/// <summary>
		/// This is called when the aiming system acquires a new target.
		/// Can be overriden. 
		/// </summary>
		/// <param name="isActive">If the target is the active target or not.</param>
		public void SetActiveTarget(bool isActive = true) {
			IsActiveTarget  = isActive;
			_targetPriority = 50;
		}

		#endregion

		#region public methods

		public void OrientToTarget(Vector3 direction, bool forward = true) {
			if (forward) {
				transform.forward = direction.normalized;
				transform.up      = Vector3.up;
			} else {
				transform.up      = direction.normalized;
				transform.forward = Vector3.forward;
			}
		}

		/// <summary>
		/// Detach Hookable if it has a parent transform.
		/// </summary>
		public void DetachFromParent() {
			Transform t = transform;
			if (t.parent) {
				_lastParent = t.parent;
				t.parent    = null;
			}
		}

		/// <summary>
		/// Reattach Hookable to a parent.
		/// </summary>
		/// <param name="newParent">The new parent transform to attach to. If null the last parent will be used if possible.</param>
		/// <param name="resetTransform"></param>
		public void AttachToParent(Transform newParent = null, bool resetTransform = false) {
			if (newParent) {
				transform.parent = newParent;
				if (resetTransform) transform.localPosition = Vector3.zero;
				return;
			}

			if (_lastParent) transform.parent = _lastParent;
		}

		public void SwitchLayerTo(string layerName) {
			int newLayer = LayerMask.NameToLayer(layerName);
			if (newLayer == -1) throw new ArgumentException("Given layer name is not valid!");

			gameObject.layer = newLayer;
		}

		#endregion

		#region protected methods

		protected virtual void OnDrawGizmos() {
			if (_anchor) {
				Gizmos.color = new Color(0, 51, 0);
				Gizmos.DrawSphere(_anchor.position, 0.1f);
			}
		}


		/// <summary>
		/// Destroy the hookable. Should always set IsHookable to false.
		/// </summary>
		protected virtual void DestroyHookable() {
			_isHookable = false;
		}

		protected void ToggleHookable(bool toggle) {
			_isHookable = toggle;
		}

		#endregion

		#region private methods

		/// <summary>
		/// Calculates a point on the hookable that is nearest to the player. 
		/// </summary>
		/// <returns>The relative position to the player.</returns>
		private Vector3 RelativePosition() {
			Collider colliderA          = this.GetComponent<Collider>();
			Vector3  playerPosition     = Player.GetPosition() + Vector3.up * Player.GetHeight() * 0.5f;
			Vector3  actualClosestPoint = colliderA.ClosestPoint(playerPosition);

			Vector3 directionToClosestPoint = new Vector3(actualClosestPoint.x - playerPosition.x, 0f, actualClosestPoint.z - playerPosition.z).normalized;
			float   dot                     = Vector3.Dot(directionToClosestPoint, Player.RawAimDirection.normalized);

			Debug.DrawLine(playerPosition, playerPosition + directionToClosestPoint, Color.magenta);
			Debug.DrawLine(playerPosition, playerPosition + Player.RawAimDirection,  Color.green);
			if (dot > 0.9f) return actualClosestPoint;

			float distance = Vector3.Distance(actualClosestPoint, playerPosition);

			Vector3 pointToCheck = new Vector3(playerPosition.x + Player.RawAimDirection.x * distance, 0f,
			                                   playerPosition.z + Player.RawAimDirection.z * distance);
			return colliderA.ClosestPoint(pointToCheck);
		}

		#endregion
	}
}
