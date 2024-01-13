using System;
using System.Collections;
using CookAGeddon.Gameplay.Hookables;
using CookAGeddon.Gameplay.Player;
using CookAGeddon.Utility;
using EasyCharacterMovement;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


namespace CookAGeddon.Gameplay.GrapplingHookUtility {
	[RequireComponent(typeof(LineRenderer))]
	public class GrapplingHook : MonoBehaviour {
		public enum States {
			Idle,
			Deploy,
			Attach,
			Retract,
			Swing
		}

		#region Serialized Fields

		[SerializeField] [Foldout("Dev")] private float _maxSwingVelocity;
		[SerializeField] [Foldout("Dev")] private float _damping;
		[SerializeField] [Foldout("Dev")] private float _k;

		///<summary>
		/// GameObject where the rope will be attached to. Can be set in the Inspector.
		///</summary>
		[Tooltip("GameObject where the rope will be attached to.  [Already set in prefab to a default position.]")]
		[SerializeField]
		private Transform _ropeEndTransform;

		///<summary>
		/// GameObject from where the grappling hook will be deployed. Can be set in the Inspector.
		///</summary>
		[Tooltip("GameObject from where the grappling hook will be deployed.")]
		[SerializeField]
		private Transform _ropeStartTransform;

		///<summary>
		/// Velocity with which the player will swing. Can be set in the Inspector.
		///</summary>
		[Tooltip("Velocity with which the player will swing.")]
		[SerializeField]
		[Foldout("Dev")]
		private float _constantSwingVelocity;

		///<summary>
		/// Since there is no movement mode for swinging until now, the Flying movement mode will be used. In this mode there is no gravity applied.
		/// Is used to scale the gravity that is applied to the player while swinging. Can be set in the inspector.
		///</summary>
		[Tooltip("Amount of gravity that is applied while swinging.")]
		[SerializeField]
		[Foldout("Dev")]
		private float _swingGravityScale;

		#endregion

		#region public properties & fields

		#region porperties

		/// <summary>
		/// If the grappling hook was attached before or not. 
		/// </summary>
		public bool WasAttachedBefore { get; set; } = false;

		/// <summary>
		/// Returns the anchor point of the swing. 
		/// </summary>
		private Vector3 SwingerPosition => _player.GetPosition() + (Vector3.up * _player.GetHeight());

		/// <summary>
		/// Returns the player center.
		/// </summary>
		private Vector3 PlayerCenterPosition => _player.GetPosition() + (Vector3.up * _player.GetHeight() * 0.5f);

		/// <summary>
		/// The GrapplingHookSetting the grappling hook should use. Is propagated from any ability that uses the grappling hook.
		/// </summary>
		public GrapplingHookSetting Setting { set; get; }

		/// <summary>
		/// The CharacterController controlling the player. 
		/// </summary>
		private HeroMovement _player;

		public bool IsIdle       => _currentState == _idleState;
		public bool IsAttached   => _currentState == _attachedState;
		public bool IsDeployed   => _currentState == _deployedState;
		public bool IsRetracting => _currentState == _retractState;
		public bool IsSwinging   => _currentState == _swingState;

		/// <summary>
		/// If a target was hit.
		/// </summary>
		public bool HitTarget => _targetHit;

		#endregion

		#endregion

		#region private properties & fields

		#region fields

		private GrapplingHookIdleState     _idleState;
		private GrapplingHookAttachedState _attachedState;
		private GrapplingHookDeployedState _deployedState;
		private GrapplingHookRetractState  _retractState;
		private GrapplingHookSwingState    _swingState;
		private GrapplingHookBaseState     _currentState;

		/// <summary>
		/// Position of the hookable anchor when the positioning of the anchor point is relative to the player.
		/// </summary>
		private Vector3 _targetPosition;

		/// <summary>
		/// Stores the used max ropeLength. 
		/// </summary>
		private float _ropeLength;

		/// <summary>
		/// Current Hookable target of the grappling hook. Should be set, before deploying the grappling hook.
		/// </summary>
		private HookableBase _currentTarget;

		/// <summary>
		/// Weather the ability is active, and states should be updated
		/// </summary>
		private bool _isActive = false;

		/// <summary>
		/// Stores the last calculated velocity while swinging.
		/// </summary>
		private Vector3 _lastVelocity;

		/// <summary>
		/// Reflection of the position where swinging is started.
		/// </summary>
		private Vector3 _swingStartReflected;

		/// <summary>
		/// Stores the relative position the grappling hook is in, before he is used.
		/// Can be used to reset the position.
		/// </summary>
		private Vector3 _relativePosition;

		/// <summary>
		/// Stores the relative rotation the grappling hook is in, before he is used.
		/// Can be used to reset the rotation.
		/// </summary>
		private Quaternion _relativeRotation;

		/// <summary>
		/// The LineRenderer Component which is used to draw the rope.
		/// </summary>
		private LineRenderer _ropeRenderer;

		/// <summary>
		/// Indicates if the grappling hook is retracting
		/// </summary>
		private bool _retracting;

		/// <summary>
		/// The actual target that was hit.
		/// </summary>
		private Transform _targetHit;

		/// <summary>
		/// Indicates weather the player is swinging or not.
		/// </summary>
		private bool _swinging = false;

		/// <summary>
		/// Used to determine if the DeployCoroutine is still running. Can also be used to stop the DeployCoroutine.
		/// </summary>
		private Coroutine _deployCoroutine;

		/// <summary>
		/// Used to determine if the RetractCoroutine is still running. Can also be used to stop the RetractCoroutine.
		/// </summary>
		private Coroutine _retractCoroutine;

		/// <summary>
		/// Used to determine if the MovePlayerThroughWaypointsCoroutine is still running. Can also be used to stop the MovePlayerThroughWaypointsCoroutine.
		/// </summary>
		private Coroutine _movePlayerThroughWaypointsCoroutine;

		/// <summary>
		/// Stores the parent the grappling hook was attached to before deployment.
		/// Can be used to reset it.
		/// </summary>
		private Transform _oldParent;

		#endregion

		#region properties

		/// <summary>
		/// Returns the current distance between the start of the rope and the grappling hook.
		/// </summary>
		private float CurrentRopeLength => Vector3.Distance(_ropeStartTransform.position, transform.position);


		/// <summary>
		/// Returns the current distance between the swing anchor and the current target.
		/// </summary>
		private float DistanceToCurrentTarget => Vector3.Distance(PlayerCenterPosition, _targetPosition);

		#endregion

		#endregion

		#region unity lifecycle methods

		private void Awake() {
			_ropeRenderer = GetComponent<LineRenderer>();

			_idleState     = new GrapplingHookIdleState();
			_deployedState = new GrapplingHookDeployedState();
			_attachedState = new GrapplingHookAttachedState();
			_retractState  = new GrapplingHookRetractState();
			_swingState    = new GrapplingHookSwingState();
			_currentState  = _idleState;

			Transform t = transform;
			if (t.parent) {
				_oldParent        = t.parent;
				_relativePosition = t.localPosition;
				_relativeRotation = t.localRotation;
			}
		}

		private void Start() {
			_player = HeroMovementReferenceGetter.Instance.GetPlayer(HeroSlot.Italian);
		}

		private void FixedUpdate() {
			if (_swinging) {
				if (_player.IsOnGround()) {
					StopSwinging();
					return;
				}
				Vector3 playerVelocity = _lastVelocity;
				Vector3 targetPosition = _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition;

				Vector3 toTarget        = targetPosition - SwingerPosition;
				float   currentDistance = toTarget.magnitude;

				// Spring calculations
				var springForce    = -_k * (_ropeLength - currentDistance);
				var springDamping  = -_damping * Vector3.Dot(playerVelocity, toTarget.normalized);
				var springNetForce = (springForce + springDamping) * toTarget.normalized;
				var springVelocity = springNetForce * Time.fixedDeltaTime;

				// only apply spring force when distance is bigger than max rope length
				// without this the player would be pushed away if he is nearer to the target than max rope length
				if (currentDistance > Setting.MaxRopeLength)
					playerVelocity += springVelocity;

				// gravity is applied here because the MovementMode is flying, and flying does not apply gravity
				// using custom swing gravity
				var gravityScaled = _player.GetGravityDirection() * _swingGravityScale;
				playerVelocity += gravityScaled * Time.fixedDeltaTime;


				// Projection of player velocity on toTarget
				float dotA = Vector3.Dot(playerVelocity, toTarget);
				float dotB = Vector3.Dot(toTarget,       toTarget);

				// actual projection
				Vector3 velocityProjection = (dotA / dotB) * toTarget;

				// Extract the component of playerVelocity that is perpendicular to toTarget.
				Vector3 orthogonalVelocity = (playerVelocity - velocityProjection).normalized;

				// add a constant amount of velocity
				playerVelocity += orthogonalVelocity * _constantSwingVelocity;

				if (playerVelocity.magnitude > _maxSwingVelocity) playerVelocity = playerVelocity.normalized * _maxSwingVelocity;

				Vector3 mirroredToTarget = (targetPosition - _swingStartReflected);
				float   inverseDot       = 1f - Vector3.Dot(toTarget.normalized, mirroredToTarget.normalized);
				// swing stop criteria 
				if (inverseDot > 0f && inverseDot <= 0.01f) {
					StopSwinging();
					return;
				}

				// set new velocity
				_lastVelocity = playerVelocity;
				_player.SetVelocity(playerVelocity);
			}
		}


		private void LateUpdate() {
			// only in attached and swing state the grappling hook should be "attached" to the current target
			if (IsAttached || IsSwinging) {
				transform.position = _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition;

				_ropeRenderer.SetPosition(0, _ropeEndTransform.position);
				_ropeRenderer.SetPosition(1, _ropeStartTransform.position);
			}

			// executes the UpdateState method when the grappling hook has been set active
			if (_isActive) {
				_currentState.UpdateState(this);
			}
		}

		#endregion

		#region public methods

		/// <summary>
		/// Wraps the SetState method.
		/// Deactivates the hook while changing states.
		/// </summary>
		/// <param name="newState">The new state the grappling hook should be in after calling this.</param>
		/// <param name="deactivate">If the grappling hook should be active after execution.</param>
		public void SwitchState(States newState, bool deactivate = false) {
			bool wasActive = _isActive;
			ToggleGrapplingHook(false);
			switch (newState) {
				case States.Idle: {
					SetState(_idleState);
					break;
				}
				case States.Attach: {
					SetState(_attachedState);
					break;
				}
				case States.Deploy: {
					SetState(_deployedState);
					break;
				}
				case States.Retract: {
					SetState(_retractState);
					break;
				}
				case States.Swing: {
					SetState(_swingState);
					break;
				}
			}

			ToggleGrapplingHook(!deactivate && wasActive);
		}

		/// <summary>
		/// Moves player between given waypoints. 
		/// </summary>
		/// <param name="waypoints"></param>
		public void MovePlayerThroughWaypoints(Vector3[] waypoints) {
			if (_movePlayerThroughWaypointsCoroutine != null) return;
			if (!IsAttached) return;
			if (waypoints.Length < 2) return;
			_movePlayerThroughWaypointsCoroutine = StartCoroutine(MovePlayerThroughWaypointsCoroutine(waypoints));
		}


		/// <summary>
		/// Launch player with CharacterController. If no direction is given, the player is launched in the direction of the current target.
		/// </summary>
		/// <param name="direction">If direction is null the player will be launched in the direction of the current target.</param>
		public void LaunchPlayer(Vector3? direction) {
			if (!direction.HasValue) {
				Vector3 directionToTarget = (_currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition) - _player.GetPosition();
				_player.PauseGroundConstraint();
				_player.LaunchCharacter(directionToTarget * Setting.PullStrength);
			} else {
				_player.PauseGroundConstraint();
				_player.LaunchCharacter(direction.Value.normalized * Setting.PullStrength);
			}

			Invoke(nameof(DelayedDetach), 1f);
		}


		/// <summary>
		/// Moves the Grappling Hook GameObject. If the Grappling Hook is attached to a parent, it is detached.
		/// Should be used as an initial placement method for the Grappling Hook.
		/// </summary>
		public void InitialPlacement() {
			if (transform.parent) transform.parent = null;

			transform.position = _ropeStartTransform.position;
		}

		/// <summary>
		/// Set the new hookable as a target. Also save the current anchor position. 
		/// </summary>
		/// <param name="newTarget">The new target that the grappling hook should use.</param>
		public void SetTarget(HookableBase newTarget) {
			_currentTarget  = newTarget;
			_targetPosition = newTarget.AnchorPosition;
		}

		/// <summary>
		/// Before starting the swing this is called to set the starting velocity. 
		/// </summary>
		public void SetStartingVelocity() {
			if (_player) {
				var     playerVelocity = _player.GetVelocity();
				Vector3 targetPosition = _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition;
				Vector3 toTarget       = targetPosition - SwingerPosition;
				_ropeLength = Setting.UseCurrentDistance ? toTarget.magnitude : Setting.MaxRopeLength;

				var gravityScaled = _player.GetGravityDirection() * _swingGravityScale;

				float dotA = Vector3.Dot(playerVelocity, toTarget);
				float dotB = Vector3.Dot(toTarget,       toTarget);

				Vector3 velocityProjection = (dotA / dotB) * toTarget;
				Vector3 orthogonalVelocity = (playerVelocity - velocityProjection).normalized * _constantSwingVelocity;

				_lastVelocity = orthogonalVelocity + gravityScaled * Time.fixedDeltaTime;
			}
		}

		/// <summary>
		/// Starts the swinging. 
		/// </summary>
		public void StartSwinging() {
			if (_swinging) return;
			if (!IsAttached) return;
			if (!_player) return;
			_player.Block();

			// Calculate the reflection of current player position along the targets up/down axis. 
			// The calculated point will be the point at which swinging should stop.
			// simulates a pendulum behaviour, which will reach the same height on out movement as it had on in movement. 
			Vector3 targetPosition            = _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition;
			Vector3 reflectionPoint           = new Vector3(targetPosition.x, SwingerPosition.y, targetPosition.z);
			float   distanceToReflectionPoint = Vector3.Distance(SwingerPosition, reflectionPoint);
			Vector3 reflectedPosition         = SwingerPosition + (reflectionPoint - SwingerPosition).normalized * (2 * distanceToReflectionPoint);
			_swingStartReflected = reflectedPosition;


			_player.SetRotationMode(RotationMode.OrientToCameraViewDirection);
			_player.SetMovementMode(MovementMode.Flying);
			_swinging = true;
			SwitchState(States.Swing);
		}

		public void StopSwinging() {
			if (!_swinging) return;
			_swinging = false;
			DelayedDetach();
			if (WasAttachedBefore) {
				TimeScaleManagerGlobal.Instance.RequestTimeScale(0.2f, 0.3f);
				LaunchPlayer(_lastVelocity);
				WasAttachedBefore = false;
			}

			_player.SetRotationMode(RotationMode.OrientToMovement);
			_player.SetMovementMode(MovementMode.Falling);

			_player.Unblock();
			if (IsSwinging)
				SwitchState(States.Retract, true);
		}


		/// <summary>
		/// Before deploying the Grappling Hook there should be no obstacle in the way. Is also a check,
		/// if the player or the grappling hook can move through without hitting anything. 
		/// </summary>
		/// <returns></returns>
		public bool IsPathClear() {
			if (!_player) return false;
			float   distance       = DistanceToCurrentTarget;
			Vector3 playerPosition = PlayerCenterPosition;
			Vector3 targetPosition = _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition;
			Vector3 rayDirection   = (targetPosition - playerPosition).normalized;
			Ray     ray            = new Ray(playerPosition, rayDirection);
			if (Physics.SphereCast(ray, _player.GetHeight() * 0.5f, out var hit, distance - _player.GetHeight() * 0.5f, Setting.ObstacleLayerMask)) {
				Transform hitObjectTransform = hit.transform;
				if (hitObjectTransform.parent) {
					if (hitObjectTransform.parent != _currentTarget.transform) return false;
					return true;
				}
			}

			return true;
		}

		/// <summary>
		/// Activated/Deactivates the hook.
		/// </summary>
		/// <param name="toggle">If the grappling hook should be active or not.</param>
		public void ToggleGrapplingHook(bool toggle = true) {
			_isActive = toggle;
		}

		/// <summary>
		/// Starts deploying the grappling hook.
		/// </summary>
		/// <returns></returns>
		public bool Deploy() {
			if (_deployCoroutine != null) return false;
			if (_retractCoroutine != null) return false;
			if (transform.parent) return false;
			_ropeRenderer.positionCount = 2;

			_ropeRenderer.SetPosition(0, _ropeStartTransform.position);
			_ropeRenderer.SetPosition(1, _ropeEndTransform.position);


			_deployCoroutine = StartCoroutine(DeployCoroutine());
			return true;
		}

		/// <summary>
		/// Starts retracting the grappling hook.
		/// </summary>
		public void Retract() {
			if (_deployCoroutine != null) {
				StopCoroutine(_deployCoroutine);
				_deployCoroutine = null;
			}
			
			if (_retractCoroutine != null) {
				return;
			}

			if (_retracting) return;

			_retracting       = true;
			_retractCoroutine = StartCoroutine(RetractCoroutine());
		}

		/// <summary>
		/// Invokes detach with a delay given by the parameter.
		/// </summary>
		/// <param name="delay">How long the delay should be.</param>
		public void DelayedDetach(float delay = 0f) {
			if (!IsAttached) return;
			if (delay == 0f) Detach();
			else
				Invoke(nameof(Detach), delay);
		}
		
		/// <summary>
		/// Detaches the hook by moving on to the retract state. Grappling hook needs to be attached before. 
		/// </summary>
		private void Detach() {
			if (!IsRetracting)
				SwitchState(States.Retract, true);
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <returns>True if the target is further away than MinTravelDistance. False otherwise.</returns>
		public bool HasMinDistance() {
			return DistanceToCurrentTarget > Setting.MinTravelDistance;
		}
		
		
		/// <summary>
		/// Resets the hook and its components. Only if hook was active before.
		/// </summary>
		public void ResetHook() {
			if (!_isActive) return;
			_isActive = false;
			
			if (_deployCoroutine != null) {
				StopCoroutine(_deployCoroutine);
				_deployCoroutine = null;
			}

			if (!transform.parent && !_retracting) {
				Retract();
			}

			if (_player)
				_player.Unblock();
			_targetHit = null;

			if (IsSwinging)
				StopSwinging();

			SwitchState(States.Idle);
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <returns>If the grappling hook has reached its max distance.</returns>
		public bool ReachedMaxDistance() {
			return CurrentRopeLength >= Setting.MaxTravelDistance;
		}


		/// <summary>
		/// When there is more than one possible target, and other targets could move into the deployed
		/// Grappling Hook, check if the correct target was hit before other actions.
		/// </summary>
		/// <returns>True: target that was hit is currentTarget. False: the target hit was not the currentTarget.</returns>
		public bool HitCorrectTarget() {
			return _targetHit == _currentTarget.transform;
		}

		#endregion

		#region private methods

		/// <summary>
		/// Set the state of the grappling hook. Exits current state and enters new one.
		/// </summary>
		/// <param name="nextState">State the grappling hook should be in next.</param>
		private void SetState(GrapplingHookBaseState nextState) {
			if (_currentState != null) {
				_currentState.ExitState(this);
			}

			_currentState = nextState;
			_currentState.EnterState(this);
		}


		/// <summary>
		/// Switches to the attach state if the grappling hook is not already attached, and a target was hit.  
		/// </summary>
		private void AttachToTarget() {
			if (IsAttached) return;
			if (!_targetHit) return;
			WasAttachedBefore = true;
			SwitchState(States.Attach);
		}
		
		/// <summary>
		/// Reset the line renderer. Could later be used to start a reset a rope simulation.
		/// </summary>
		private void ResetLineRenderer() {
			_ropeRenderer.positionCount = 0;
		}
		
		/// <summary>
		/// Reset the grappling hook to its parent. Parent could be the gun that shoots the grappling hook.
		/// </summary>
		private void ResetToParent() {
			Transform t = transform;
			if (_oldParent) {
				t.parent        = _oldParent;
				t.localPosition = _relativePosition;
				t.localRotation = _relativeRotation;
			}
		}

		#endregion

		#region coroutines

		private IEnumerator MovePlayerThroughWaypointsCoroutine(Vector3[] waypoints) {
			foreach (var waypoint in waypoints) {
				_player.EnableGravity(false);
				_player.PauseGroundConstraint();
				float distance = Vector3.Distance(_player.GetPosition(), waypoint);
				var   time     = (distance / Setting.PullStrength) * 0.99f;
				float t        = 0;
				while (t < time) {
					_player.SetPosition(Vector3.MoveTowards(_player.GetPosition(), waypoint, Setting.PullStrength * Time.fixedUnscaledDeltaTime));
					t += Time.fixedUnscaledDeltaTime;
					yield return null;
				}
			}

			_player.EnableGravity(true);
			_movePlayerThroughWaypointsCoroutine = null;
			DelayedDetach();
			_player.Unblock();
		}

		private IEnumerator DeployCoroutine() {
			while (!_targetHit) {
				var t = transform;
				t.position = Vector3.MoveTowards(t.position, _currentTarget.IsAnchorRelative ? _targetPosition : _currentTarget.AnchorPosition,
				                                 Setting.DeploySpeed * Time.fixedUnscaledDeltaTime);
				t.rotation = Quaternion.RotateTowards(t.rotation,
				                                      Quaternion
					                                     .LookRotation((_currentTarget.IsAnchorRelative ? _targetPosition - t.position : _currentTarget.AnchorPosition - t.position).normalized,
					                                                   t.up), 1000f);
				_ropeRenderer.SetPosition(0, _ropeStartTransform.position);
				_ropeRenderer.SetPosition(1, _ropeEndTransform.position);
				yield return null;
			}

			AttachToTarget();


			_deployCoroutine = null;
		}

		private IEnumerator RetractCoroutine() {
			
			while (_retracting) {
				if (_ropeRenderer.positionCount == 0) break;
				if (!Setting.AnimateRetracting) break;
				Vector3 endPosition = _ropeStartTransform.position;
				transform.position = Vector3.MoveTowards(transform.position, endPosition, Setting.RetractSpeed * Time.fixedUnscaledDeltaTime);
				_ropeRenderer.SetPosition(0, endPosition);
				_ropeRenderer.SetPosition(1, _ropeEndTransform.position);

				yield return null;
			}


			_retractCoroutine = null;
			ToggleGrapplingHook(false);
			SwitchState(States.Idle);

			ResetToParent();
			ResetLineRenderer();
		}

		#endregion

		#region unity event handlers

		private void OnTriggerEnter(Collider other) {
			// Check if the grappling hook is retracting and has collided with the retract trigger
			// If so, stop retracting and exit the function
			// Otherwise, continue with the rest of the code
			if (_retracting && other.gameObject.layer == LayerMask.NameToLayer("RetractTrigger")) {
				_retracting = false;
				return;
			}

			// Check if the grappling hook hit a GrapplingHookTrigger and _currentTarget is not null
			// If this happens outside of the Deploy-State do not register the new hit. 
			if (other.gameObject.layer == LayerMask.NameToLayer("GrapplingHookTrigger") && _currentTarget) {
				if (!IsDeployed) return;

				// Actual target is always the parent of the grappling hook trigger
				_targetHit = other.transform.parent;
			}
		}
#if UNITY_EDITOR
		private void OnDrawGizmosSelected() {
			Gizmos.color = Color.green;
			if (_ropeEndTransform)
				Gizmos.DrawSphere(_ropeEndTransform.position, 0.02f);
		}

		private void OnDrawGizmos() {
			Gizmos.color = Color.black;
			Gizmos.DrawSphere(_swingStartReflected, 0.1f);
		}
#endif

		#endregion
	}
}
