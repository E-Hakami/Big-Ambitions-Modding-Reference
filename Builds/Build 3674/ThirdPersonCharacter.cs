using System;
using System.Collections;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Characters;
using Characters.EmojiSystem;
using Controllers;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;

public class ThirdPersonCharacter : BaseHuman
{
	public enum WalkingSpeed
	{
		Zombie,
		Walk,
		Jog,
		Run,
		Scooter
	}

	private const float TimeForStoppingWhenPathIsPending = 2f;

	private const float HandIkMaxDistance = 0.5f;

	private const float HandIkMaxDistanceSqr = 0.25f;

	[SerializeField]
	private SingleCoroutineStarterStopper npcExpressionsSingleCoroutineStarterStopper;

	[SerializeField]
	private BoredAnimations boredAnimations;

	public float m_MovingTurnSpeed = 360f;

	public float m_StationaryTurnSpeed = 180f;

	public float m_RotationSpeed = 10f;

	public bool updatePlayerPos = true;

	private static readonly int GlobalPlayerPosID = Shader.PropertyToID("GlobalPlayerPos");

	private Vector3 m_GroundNormal;

	public CapsuleCollider capsuleCollider;

	public NavMeshAgent navmeshAgent;

	public Vector3 LookTarget;

	private NavMeshObstacle _obstacle;

	public bool isWalkingTowardsTarget;

	public float distanceToCurrentTarget;

	public bool isPlayer;

	public WalkingSpeed walkingSpeed;

	public Rigidbody characterRigidbody;

	private EntityController _entityController;

	[NonSerialized]
	public bool isKinematic;

	private Transform _selectedGender;

	private Transform RHandIKAttachmentTarget;

	private Transform LHandIKAttachmentTarget;

	private Transform _headIKAttachmentTarget;

	private Tweener _tweenHandL;

	private Tweener _tweenHandR;

	private Tweener _tweenHead;

	private TwoBoneIKConstraint _constraintΗandL;

	private TwoBoneIKConstraint _constraintHandR;

	private bool _ikOnItemController;

	public Vector3 velocity;

	public static bool permanentZombieWalking;

	public CharacterEmojiExpression characterEmojiExpression;

	private CoroutineQueue _playerExpressionsQueue;

	private static HappinessBoostEmojiShower _happinessBoostEmojiShower;

	[NonSerialized]
	public bool visible = true;

	private Vector3 _lastInputVelocity;

	private bool _wasPathPendingLastFrame;

	private float _timerToStopMovement;

	private bool _hasBoredAnimations;

	private bool _isUsingIKs;

	private ItemController _handContentController;

	private RuntimeAnimatorController _motionTimeParamCheckedOn;

	private bool _hasMotionTimeParam;

	private TweenerCore<Quaternion, Quaternion, NoOptions> _rotateTowardsTweener;

	private static float WalkingSpeedZombie => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedZombie;

	private static float WalkingSpeedWalk => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedWalk;

	private static float WalkingSpeedWalkFast => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedWalkFast;

	private static float WalkingSpeedJog => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedJog;

	private static float WalkingSpeedRun => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedRun;

	private static float WalkingSpeedScooter => InstanceBehavior<GlobalReferences>.Instance.walkingSpeedScooter;

	private static float AnimSpeedWalk => InstanceBehavior<GlobalReferences>.Instance.animationSpeedWalk;

	private static float AnimSpeedWalkFast => InstanceBehavior<GlobalReferences>.Instance.animationSpeedWalkFast;

	private static float AnimSpeedJog => InstanceBehavior<GlobalReferences>.Instance.animationSpeedJog;

	private static float AnimSpeedRun => InstanceBehavior<GlobalReferences>.Instance.animationSpeedRun;

	public bool WasLastTargetReached { get; private set; }

	public bool IsRunning => walkingSpeed == WalkingSpeed.Run;

	public bool IsZombieWalking => walkingSpeed == WalkingSpeed.Zombie;

	public EntityController CurrentEntityController => _entityController;

	public float CurrentVelocity { get; private set; }

	public float TurnAngle { get; private set; }

	private void Start()
	{
		if (isPlayer && SaveGameManager.Current != null)
		{
			InitHappinessEmojiBooster();
			navmeshAgent.speed = (IsRunning ? GetSpeed(WalkingSpeed.Run) : GetSpeed(WalkingSpeed.Jog));
			permanentZombieWalking = SaveGameManager.Current.charactersData.Count > 0 && PlayerHelper.ShouldEnablePermanentZombieWalking();
			GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
			{
				CoroutineUtility.RunAfterOneFrame(delegate
				{
					SetWalkingSpeed(walkingSpeed);
				});
			});
		}
		if (boredAnimations != null)
		{
			_hasBoredAnimations = true;
			boredAnimations.enabled = false;
		}
		else
		{
			_hasBoredAnimations = false;
		}
		RigBuilder rigBuilder = GetRigBuilder();
		if (rigBuilder != null)
		{
			rigBuilder.enabled = _isUsingIKs;
		}
		_constraintΗandL = appearanceSetter.leftHandIKRig.GetComponentInChildren<TwoBoneIKConstraint>();
		_constraintHandR = appearanceSetter.rightHandIKRig.GetComponentInChildren<TwoBoneIKConstraint>();
	}

	public void InitPlayerExpressionsQueue()
	{
		if (_playerExpressionsQueue == null && InstanceBehavior<GameManager>.Instance != null)
		{
			_playerExpressionsQueue = new CoroutineQueue(InstanceBehavior<GameManager>.Instance);
		}
	}

	private void InitHappinessEmojiBooster()
	{
		if (_happinessBoostEmojiShower == null)
		{
			_happinessBoostEmojiShower = new HappinessBoostEmojiShower(this, 3f);
		}
		else
		{
			_happinessBoostEmojiShower.SetTpc(this);
		}
	}

	public void RotateTowards(Vector3 target)
	{
		Vector3 forward = target - base.transform.position;
		forward.y = 0f;
		Quaternion b = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * m_RotationSpeed);
	}

	public IEnumerator RotateTowards(Vector3 target, float duration)
	{
		Vector3 forward = target - base.transform.position;
		forward.y = 0f;
		Quaternion endValue = Quaternion.LookRotation(forward);
		_rotateTowardsTweener = base.transform.DORotateQuaternion(endValue, duration).SetLink(base.gameObject);
		yield return _rotateTowardsTweener.WaitForCompletion();
	}

	public IEnumerator RotateTowards(Quaternion targetRotation, float duration)
	{
		_rotateTowardsTweener = base.transform.DORotateQuaternion(targetRotation, duration).SetLink(base.gameObject);
		yield return _rotateTowardsTweener.WaitForCompletion();
	}

	private void Awake()
	{
		navmeshAgent.avoidancePriority = (isPlayer ? 99 : UnityEngine.Random.Range(1, 99));
		velocity = Vector3.zero;
		_lastInputVelocity = Vector3.zero;
		if (isPlayer)
		{
			GameInstance current = SaveGameManager.Current;
			if (current != null && current.charactersData.Count > 0)
			{
				appearanceSetter.SetAppearance(SaveGameManager.Current.charactersData.First());
				appearanceSetter.UpdateVisualAge();
			}
		}
	}

	private void Update()
	{
		if (LoadScene.isLoading)
		{
			return;
		}
		if (isPlayer)
		{
			_playerExpressionsQueue.Update();
			_happinessBoostEmojiShower.Update();
		}
		if (navmeshAgent.isActiveAndEnabled && navmeshAgent.isOnNavMesh)
		{
			if (InstanceBehavior<UIs>.Instance != null && !InstanceBehavior<UIs>.Instance.gameSpeed.Paused && Time.deltaTime != 0f)
			{
				if (isPlayer)
				{
					Move(navmeshAgent.hasPath ? navmeshAgent.velocity : velocity);
				}
				else
				{
					HandleNpcMovement();
				}
			}
		}
		else
		{
			StopMovement();
			if (LookTarget != Vector3.zero)
			{
				RotateTowards(LookTarget);
			}
		}
		if (_isUsingIKs)
		{
			if ((bool)RHandIKAttachmentTarget)
			{
				appearanceSetter.rightHandIKTarget.position = RHandIKAttachmentTarget.position;
				appearanceSetter.rightHandIKTarget.rotation = RHandIKAttachmentTarget.rotation;
				Transform parent = rightHand.parent.parent;
				Vector3 vector = RHandIKAttachmentTarget.position - parent.position;
				_constraintΗandL.weight = ((isPlayer || !(vector.sqrMagnitude > 0.25f)) ? 1 : 0);
			}
			if ((bool)LHandIKAttachmentTarget)
			{
				appearanceSetter.leftHandIKTarget.position = LHandIKAttachmentTarget.position;
				appearanceSetter.leftHandIKTarget.rotation = LHandIKAttachmentTarget.rotation;
				Transform parent2 = leftHand.parent.parent;
				Vector3 vector2 = LHandIKAttachmentTarget.position - parent2.position;
				_constraintHandR.weight = ((isPlayer || !(vector2.sqrMagnitude > 0.25f)) ? 1 : 0);
			}
			if ((bool)_headIKAttachmentTarget)
			{
				appearanceSetter.headIKTarget.position = _headIKAttachmentTarget.position;
				appearanceSetter.headIKTarget.rotation = _headIKAttachmentTarget.rotation;
			}
		}
		if (updatePlayerPos && isPlayer)
		{
			Vector4 value = capsuleCollider.transform.position;
			Shader.SetGlobalVector(GlobalPlayerPosID, value);
		}
	}

	private void StopMovement()
	{
		Move(Vector3.zero);
		_lastInputVelocity = Vector3.zero;
		_wasPathPendingLastFrame = false;
		_timerToStopMovement = 0f;
	}

	private void HandleNpcMovement()
	{
		if (navmeshAgent.isStopped)
		{
			Move(navmeshAgent.velocity);
			_lastInputVelocity = navmeshAgent.velocity;
			_wasPathPendingLastFrame = false;
			_timerToStopMovement = 0f;
			return;
		}
		if (navmeshAgent.pathPending)
		{
			if (_timerToStopMovement >= 2f)
			{
				StopMovement();
				navmeshAgent.updateRotation = false;
				return;
			}
			Move(_lastInputVelocity, forceMovement: true);
			navmeshAgent.velocity = Vector3.zero;
			_wasPathPendingLastFrame = true;
			_timerToStopMovement += Time.deltaTime;
			return;
		}
		navmeshAgent.updateRotation = true;
		_timerToStopMovement = 0f;
		if (_wasPathPendingLastFrame)
		{
			_wasPathPendingLastFrame = false;
			navmeshAgent.velocity = _lastInputVelocity.normalized * navmeshAgent.speed;
			Move(_lastInputVelocity);
		}
		else
		{
			Move(navmeshAgent.velocity);
			_lastInputVelocity = navmeshAgent.velocity;
		}
	}

	public void Move(Vector3 inputVelocity, bool forceMovement = false)
	{
		if (isPlayer)
		{
			if (!JobHelper.IsPlayerWorking() && inputVelocity.sqrMagnitude > 0.1f)
			{
				EnergyHelper.AddEnergySpender("move", EnergyConsumption.Minimal);
			}
			else
			{
				EnergyHelper.RemoveEnergySpender("move");
			}
			if (VehicleHelper.GetCurrentVehicleBase() is HandTruck handTruck)
			{
				handTruck.SetAnimSpeed(inputVelocity.sqrMagnitude);
			}
		}
		float num = (CurrentVelocity = inputVelocity.magnitude);
		if (num <= 0.01f)
		{
			animator.SetBool(BaseHuman.IsMoving, value: false);
			if (_hasBoredAnimations && !boredAnimations.enabled)
			{
				boredAnimations.enabled = true;
			}
			return;
		}
		if (_hasBoredAnimations && boredAnimations.enabled)
		{
			boredAnimations.enabled = false;
		}
		if (forceMovement)
		{
			base.transform.position += inputVelocity * Time.deltaTime;
		}
		Vector3 normalized = inputVelocity.normalized;
		normalized = base.transform.InverseTransformDirection(normalized);
		normalized = Vector3.ProjectOnPlane(normalized, m_GroundNormal);
		TurnAngle = Mathf.Atan2(normalized.x, normalized.z);
		float num2 = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, num);
		if (Mathf.Abs(TurnAngle * num2) > 0.1f)
		{
			base.transform.Rotate(0f, TurnAngle * num2 * Time.deltaTime, 0f);
		}
		float num3 = Mathf.Min(num, 6f);
		if (animator.GetBool(BaseHuman.IsMoving))
		{
			num3 = Mathf.MoveTowards(animator.GetFloat(BaseHuman.Forward), num3, 20f * Time.deltaTime);
		}
		animator.SetFloat(BaseHuman.Forward, num3);
		animator.SetBool(BaseHuman.IsMoving, value: true);
		if (_motionTimeParamCheckedOn != animator.runtimeAnimatorController)
		{
			_motionTimeParamCheckedOn = animator.runtimeAnimatorController;
			_hasMotionTimeParam = animator.HasParameter(BaseHuman.MotionTime);
		}
		if (_hasMotionTimeParam)
		{
			float num4;
			if (num3 < WalkingSpeedWalkFast)
			{
				float t = Mathf.InverseLerp(WalkingSpeedWalk, WalkingSpeedWalkFast, num3);
				num4 = Mathf.Lerp(AnimSpeedWalk, AnimSpeedWalkFast, t);
			}
			else if (num3 < WalkingSpeedJog)
			{
				float t2 = Mathf.InverseLerp(WalkingSpeedWalkFast, WalkingSpeedJog, num3);
				num4 = Mathf.Lerp(AnimSpeedWalkFast, AnimSpeedJog, t2);
			}
			else
			{
				float t3 = Mathf.InverseLerp(WalkingSpeedJog, WalkingSpeedRun, num3);
				num4 = Mathf.Lerp(AnimSpeedJog, AnimSpeedRun, t3);
			}
			float num5 = animator.GetFloat(BaseHuman.MotionTime);
			animator.SetFloat(BaseHuman.MotionTime, num5 + Time.deltaTime * num4);
		}
	}

	public void ToggleRunning(bool running, bool force = false)
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null && !force)
		{
			return;
		}
		WalkingSpeed walkingSpeed = (running ? WalkingSpeed.Run : WalkingSpeed.Jog);
		if (ShouldEnableZombieWalking())
		{
			walkingSpeed = WalkingSpeed.Zombie;
		}
		else if (SaveGameManager.Current.Energy <= 0f)
		{
			walkingSpeed = WalkingSpeed.Jog;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if ((object)selectedVehicle != null && selectedVehicle.vehicleType.HasTag(TagRef.Vehicletag.isscooter))
		{
			walkingSpeed = WalkingSpeed.Scooter;
		}
		bool flag = walkingSpeed != this.walkingSpeed;
		if (!flag && !force)
		{
			return;
		}
		if (flag && SaveGameManager.Current.Energy <= 0f)
		{
			switch (walkingSpeed)
			{
			case WalkingSpeed.Zombie:
				Notifications.ShowError("notification_no_energy_to_walk", "notification_no_energy_to_walk");
				break;
			case WalkingSpeed.Run:
				Notifications.Show(NotificationType.Warning, "notification_no_energy_to_run", null, 4f, "notification_no_energy_to_run");
				break;
			}
			if (IsDancing())
			{
				StopDancing();
			}
		}
		SetWalkingSpeed(walkingSpeed);
	}

	private static bool ShouldEnableZombieWalking()
	{
		if (!permanentZombieWalking)
		{
			if (SaveGameManager.Current.Energy <= 0f)
			{
				return SaveGameManager.Current.Hunger <= 0f;
			}
			return false;
		}
		return true;
	}

	public void SetWalkingSpeed(WalkingSpeed speed)
	{
		walkingSpeed = speed;
		navmeshAgent.speed = GetSpeed(speed);
		animator.SetBool(BaseHuman.Zombie, speed == WalkingSpeed.Zombie);
	}

	private float GetSpeed(WalkingSpeed speed)
	{
		return speed switch
		{
			WalkingSpeed.Zombie => WalkingSpeedZombie, 
			WalkingSpeed.Walk => WalkingSpeedWalk, 
			WalkingSpeed.Jog => WalkingSpeedJog, 
			WalkingSpeed.Run => WalkingSpeedRun, 
			WalkingSpeed.Scooter => WalkingSpeedScooter, 
			_ => throw new ArgumentOutOfRangeException("speed", speed, null), 
		};
	}

	public Transform GetHandContent()
	{
		if (!(_handContentController != null))
		{
			return null;
		}
		return _handContentController.transform;
	}

	public void UpdateHandContentVisuals(int orderEntriesGrabbed)
	{
		Transform transform = GetHandContent();
		if (transform == null)
		{
			return;
		}
		transform.Find("Products1")?.gameObject.SetActive(value: false);
		transform.Find("Products2")?.gameObject.SetActive(value: false);
		float num = (float)orderEntriesGrabbed / 4f;
		if (num > 0f)
		{
			if (num <= 0.5f)
			{
				transform.Find("Products1")?.gameObject.SetActive(value: true);
			}
			else
			{
				transform.Find("Products2")?.gameObject.SetActive(value: true);
			}
		}
	}

	public void SetHandIKTargets(Transform LHandTarget, Transform RHandTarget, bool smooth = false)
	{
		_ikOnItemController = false;
		LHandIKAttachmentTarget = LHandTarget;
		RHandIKAttachmentTarget = RHandTarget;
		SetIsUsingIKs();
		if ((bool)appearanceSetter.leftHandIKRig)
		{
			AnimateRigWeight(LHandTarget ? 1f : 0f, appearanceSetter.leftHandIKRig, ref _tweenHandL, smooth);
		}
		if ((bool)appearanceSetter.rightHandIKRig)
		{
			AnimateRigWeight(RHandTarget ? 1f : 0f, appearanceSetter.rightHandIKRig, ref _tweenHandR, smooth);
		}
	}

	public void SetHeadIKTarget(Transform target, bool smooth = false)
	{
		_ikOnItemController = false;
		_headIKAttachmentTarget = target;
		SetIsUsingIKs();
		if ((bool)appearanceSetter.headIKRig)
		{
			AnimateRigWeight(target ? 1f : 0f, appearanceSetter.headIKRig, ref _tweenHead, smooth);
		}
	}

	public void SetItemIKTargets(ItemController itemController, bool smooth = false)
	{
		SetHandIKTargets(itemController ? itemController.LHandIKAttachmentPoint : null, itemController ? itemController.RHandIKAttachmentPoint : null, smooth);
		SetHeadIKTarget(itemController ? itemController.HeadIKAttachmentPoint : null, smooth);
		_ikOnItemController = itemController;
	}

	public void ReleaseItemIKTargets()
	{
		if (_ikOnItemController)
		{
			SetHandIKTargets(null, null);
			SetHeadIKTarget(null);
			_ikOnItemController = false;
		}
	}

	private void SetIsUsingIKs()
	{
		_isUsingIKs = (bool)LHandIKAttachmentTarget || (bool)RHandIKAttachmentTarget || (bool)_headIKAttachmentTarget;
		RigBuilder rigBuilder = GetRigBuilder();
		if (rigBuilder != null)
		{
			rigBuilder.enabled = _isUsingIKs;
		}
	}

	private RigBuilder GetRigBuilder()
	{
		if (appearanceSetter.rigBuilder == null)
		{
			appearanceSetter.rigBuilder = appearanceSetter.GetComponentInChildren<RigBuilder>(includeInactive: true);
		}
		return appearanceSetter.rigBuilder;
	}

	private static void AnimateRigWeight(float targetWeight, Rig rig, ref Tweener tween, bool smooth)
	{
		if (tween != null && tween.IsActive())
		{
			tween.Kill();
		}
		float duration = (smooth ? 0.5f : 0.1f);
		tween = DOTween.To(() => rig.weight, delegate(float x)
		{
			rig.weight = x;
		}, targetWeight, duration);
	}

	public void SetHandContent(Transform obj)
	{
		if (_handContentController != null)
		{
			InstanceBehavior<BuildingManager>.Instance.allItemControllers?.Remove(_handContentController);
			UnityEngine.Object.Destroy(_handContentController.gameObject);
			_handContentController = null;
		}
		if ((bool)obj)
		{
			ItemController itemController = (_handContentController = obj.GetComponent<ItemController>());
			_handContentController.TogglePhysics(physicsEnabled: false);
			obj.transform.SetParent(handContent);
			obj.transform.localPosition = itemController.Item.playerMountPosition;
			obj.transform.localEulerAngles = itemController.Item.playerMountRotation;
			SetHandIKTargets(itemController.LHandIKAttachmentPoint, itemController.RHandIKAttachmentPoint);
			if (itemController.Item.playGrabSound)
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(itemController.Item.grabSoundType, obj.position, 1f, isPlayer);
			}
			if (!visible)
			{
				HideHandContent();
			}
		}
		else
		{
			SetHandIKTargets(null, null);
		}
		if (isPlayer)
		{
			CoroutineUtility.RunAfterOneFrame(UpdateZombieHoldingBoxState);
		}
	}

	public void UpdateZombieHoldingBoxState()
	{
		bool value = InstanceBehavior<GameManager>.Instance.selectedVehicle != null || GetHandContent() != null;
		animator.SetBool(BaseHuman.HoldingBox, value);
	}

	public IEnumerator MoveToPosition(Transform lookTarget, Vector3 position, float maxDistance = 0.5f, bool rotateToLookTarget = false, UnityAction onPositionReached = null, float delay = 0f, UnityAction onPositionReachedBeforeRotation = null, bool abortIfStuck = false)
	{
		return MoveToPosition(lookTarget?.position ?? Vector3.zero, position, maxDistance, rotateToLookTarget, onPositionReached, delay, onPositionReachedBeforeRotation, abortIfStuck);
	}

	public IEnumerator MoveToPosition(Vector3 lookTarget, Vector3 position, float maxDistance = 0.5f, bool rotateToLookTarget = false, UnityAction onPositionReached = null, float delay = 0f, UnityAction onPositionReachedBeforeRotation = null, bool abortIfStuck = false)
	{
		isWalkingTowardsTarget = true;
		WasLastTargetReached = false;
		LookTarget.y = base.transform.position.y;
		yield return new WaitForSeconds(delay);
		if (navmeshAgent != null && navmeshAgent.isActiveAndEnabled)
		{
			NavMeshPath path = new NavMeshPath();
			navmeshAgent.CalculatePath(position, path);
			navmeshAgent.SetPath(path);
			LookTarget = lookTarget;
		}
		else
		{
			rotateToLookTarget = false;
			LookTarget = Vector3.zero;
		}
		int stuckFrames = 0;
		Vector3 lastPosition = navmeshAgent.transform.position;
		yield return new WaitUntil(delegate
		{
			if (navmeshAgent == null || !navmeshAgent.isActiveAndEnabled)
			{
				return true;
			}
			if (abortIfStuck && !InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
			{
				if (Vector3.SqrMagnitude(lastPosition - navmeshAgent.transform.position) < 0.01f)
				{
					stuckFrames++;
					if (stuckFrames > 40)
					{
						return true;
					}
				}
				else
				{
					stuckFrames = 0;
					lastPosition = base.transform.position;
				}
			}
			distanceToCurrentTarget = Vector3.SqrMagnitude(navmeshAgent.transform.position - position);
			return distanceToCurrentTarget <= maxDistance * maxDistance;
		});
		if (navmeshAgent == null || !navmeshAgent.isActiveAndEnabled)
		{
			yield break;
		}
		WasLastTargetReached = true;
		onPositionReachedBeforeRotation?.Invoke();
		if (rotateToLookTarget)
		{
			yield return new WaitUntil(() => navmeshAgent == null || !navmeshAgent.isActiveAndEnabled || navmeshAgent.velocity.sqrMagnitude == 0f);
			if (navmeshAgent == null || !navmeshAgent.isActiveAndEnabled)
			{
				yield break;
			}
			Vector3 forward = lookTarget - base.transform.position;
			forward.y = 0f;
			float num = (Mathf.Abs(Quaternion.Angle(Quaternion.LookRotation(forward), base.transform.rotation)) + 180f) % 360f - 180f;
			num = ((num < -180f) ? (num + 360f) : num);
			yield return RotateTowards(lookTarget, 1f * (num / 360f));
		}
		onPositionReached?.Invoke();
		isWalkingTowardsTarget = false;
	}

	public IEnumerator ShowExpression(CharacterEmojiName characterEmojiName, float secondsToShow = 1f, object localizationArgs = null)
	{
		if (!isPlayer && (!BuildingManager.IsInsideBuilding || InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness))
		{
			if (characterEmojiExpression != null && characterEmojiExpression.gameObject.activeSelf && characterEmojiExpression.inWorldTarget == head)
			{
				npcExpressionsSingleCoroutineStarterStopper.StopActiveCoroutine();
				characterEmojiExpression.Release();
			}
			npcExpressionsSingleCoroutineStarterStopper.StartNewCoroutine(CharacterEmojiSystem.ShowEmoji(head, characterEmojiName, showText: true, secondsToShow, localizationArgs, SetCharacterEmojiExpression));
			yield return new WaitForSeconds(secondsToShow);
		}
	}

	private void SetCharacterEmojiExpression(CharacterEmojiExpression emojiExpression)
	{
		characterEmojiExpression = emojiExpression;
	}

	public void EnqueuePlayerExpression(CharacterEmojiName characterEmojiName, float secondsToShow = 1f)
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			_playerExpressionsQueue.AddCoroutine(CharacterEmojiSystem.ShowEmoji(head, characterEmojiName, showText: false, secondsToShow));
		}
	}

	public void ClearPlayerExpressionsQueue()
	{
		_playerExpressionsQueue?.Clear();
	}

	public void ToggleVisibility(bool show, bool includePhysics = true)
	{
		if (includePhysics)
		{
			navmeshAgent.enabled = show;
			if ((bool)capsuleCollider)
			{
				capsuleCollider.enabled = show;
			}
			base.gameObject.SetActive(show);
			isKinematic = !show;
			if ((bool)characterRigidbody)
			{
				characterRigidbody.isKinematic = !show;
			}
			if (!show && (bool)capsuleCollider)
			{
				Manager.TriggerColliderRemovedEvent(capsuleCollider);
			}
		}
		base.transform.localScale = (show ? Vector3.one : Vector3.zero);
	}

	public Vector3 GetRandomPosition(float radius = 5f, float offset = 0f)
	{
		for (int i = 0; i < 30; i++)
		{
			if (NavMesh.SamplePosition(base.transform.position + base.transform.forward * offset + UnityEngine.Random.insideUnitSphere * radius, out var hit, 1f, -1))
			{
				return hit.position;
			}
		}
		return base.transform.position;
	}

	public void Bump(float range = 0.5f)
	{
		navmeshAgent.SetDestination(GetRandomPosition(range));
	}

	public void ForceToTransform(Transform target)
	{
		ForceToPosition(target.position);
		ForceToRotation(target.rotation);
	}

	public void ForceToPosition(Vector3 position)
	{
		if (navmeshAgent.isOnNavMesh)
		{
			navmeshAgent.ResetPath();
		}
		LookTarget = Vector3.zero;
		navmeshAgent.updatePosition = false;
		navmeshAgent.updateRotation = false;
		isKinematic = true;
		if ((bool)characterRigidbody)
		{
			characterRigidbody.isKinematic = true;
		}
		navmeshAgent.transform.position = position;
	}

	public void ForceToRotation(Quaternion rotation)
	{
		navmeshAgent.transform.rotation = rotation;
	}

	public override void Reset()
	{
		navmeshAgent.updatePosition = true;
		navmeshAgent.updateRotation = true;
		navmeshAgent.enabled = true;
		if ((bool)isSittingOn)
		{
			SeatController componentInParent = isSittingOn.GetComponentInParent<SeatController>();
			if ((bool)componentInParent)
			{
				componentInParent.OnSittingChanged(isSittingOn, isSitting: false);
			}
			isSittingOn = null;
			onSittingChanged?.Invoke(obj: false);
		}
		if (isPlayer && ShouldEnableZombieWalking())
		{
			animator.SetBool(BaseHuman.Zombie, value: true);
		}
		base.Reset();
		if ((bool)_entityController)
		{
			if (isKinematic)
			{
				navmeshAgent.Warp(_entityController.GetNavMeshTargetPosition());
			}
			_entityController = null;
		}
		isKinematic = false;
		if ((bool)characterRigidbody)
		{
			characterRigidbody.isKinematic = false;
		}
		StopMovement();
	}

	public void LinkToPointAndClickObject(EntityController entityController)
	{
		_entityController = entityController;
	}

	private void OnDestroy()
	{
		_rotateTowardsTweener?.Kill();
		if (isPlayer)
		{
			_playerExpressionsQueue = null;
			_happinessBoostEmojiShower?.Disable();
		}
	}

	public void WarpSafely(Vector3 target)
	{
		if (NavMesh.SamplePosition(target, out var hit, 3f, -1))
		{
			navmeshAgent.Warp(hit.position);
		}
		else
		{
			Debug.LogWarning("WarpSafely failed for character", base.gameObject);
		}
	}

	public void ResetZombieState()
	{
		animator.SetBool(BaseHuman.Zombie, value: false);
		animator.SetBool(BaseHuman.HoldingBox, value: false);
	}

	public override void LayOnBed(EntityController bed, Transform sleepPositionTransform)
	{
		_entityController = bed;
		ForceToTransform(sleepPositionTransform);
		base.LayOnBed(bed, sleepPositionTransform);
	}

	public override void SitOnChair(Transform chair, PermanentAnimationType animationType = PermanentAnimationType.Sitting)
	{
		LookTarget = Vector3.zero;
		navmeshAgent.updatePosition = false;
		navmeshAgent.updateRotation = false;
		navmeshAgent.enabled = false;
		_rotateTowardsTweener?.Complete();
		if (isPlayer)
		{
			animator.SetBool(BaseHuman.Zombie, value: false);
		}
		base.SitOnChair(chair, animationType);
		isKinematic = true;
		if ((bool)characterRigidbody)
		{
			characterRigidbody.isKinematic = true;
		}
		if (isPlayer)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ChairSitDown, base.transform.position, 1f, isPlayerCreatedSound: true);
		}
	}

	protected override void Freeze()
	{
		base.Freeze();
		navmeshAgent.isStopped = true;
		isKinematic = true;
		if ((bool)characterRigidbody)
		{
			characterRigidbody.isKinematic = true;
		}
	}

	public override void UnFreeze()
	{
		base.UnFreeze();
		navmeshAgent.isStopped = false;
		isKinematic = false;
		characterRigidbody.isKinematic = false;
		navmeshAgent.updatePosition = true;
		navmeshAgent.updateRotation = true;
	}

	public void ShowHandContent()
	{
		if (!visible || _handContentController == null)
		{
			return;
		}
		Renderer[] renderers = _handContentController.Renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = true;
		}
		foreach (Renderer shadowCaster in _handContentController.GetShadowCasters())
		{
			shadowCaster.enabled = true;
		}
		SetHandIKTargets(_handContentController.LHandIKAttachmentPoint, _handContentController.RHandIKAttachmentPoint);
	}

	public void HideHandContent()
	{
		if (_handContentController == null)
		{
			return;
		}
		Renderer[] renderers = _handContentController.Renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = false;
		}
		foreach (Renderer shadowCaster in _handContentController.GetShadowCasters())
		{
			shadowCaster.enabled = false;
		}
		SetHandIKTargets(null, null);
	}

	public virtual void OnVisibleChanged(bool newVisible)
	{
		if (newVisible != visible)
		{
			visible = newVisible;
			animator.enabled = newVisible;
			appearanceSetter.lastMergedMeshRenderer.enabled = newVisible;
			shadowCaster.enabled = newVisible;
			ToggleAllAttachedObjects(newVisible);
			if (newVisible)
			{
				ShowHandContent();
			}
			else
			{
				HideHandContent();
			}
		}
	}

	public void EnableHappinessBoostEmojiShower()
	{
		_happinessBoostEmojiShower.Enable();
	}

	public void DisableHappinessBoostEmojiShower()
	{
		_happinessBoostEmojiShower.Disable();
	}

	public void DestroyBoredAnimations()
	{
		if (!isPlayer)
		{
			if (boredAnimations != null)
			{
				UnityEngine.Object.Destroy(boredAnimations);
			}
			boredAnimations = null;
		}
	}
}
