using System;
using System.Collections.Generic;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UI;
using UI.Notification;
using UnityEngine;

public class HandTruck : VehicleController
{
	private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");

	private static readonly int UsingHands = Animator.StringToHash("UsingHands");

	public Transform boxPlaceholder;

	public float playerOffsetWhenActive;

	public float mainVolume;

	public float minVolume;

	public float maxVolume;

	public float walkingPitch;

	public float runningPitch;

	public float zombiePitch;

	public float pitchFadeSpeed;

	[BoxGroup("IK")]
	public Transform LHandIKAttachmentPoint;

	[BoxGroup("IK")]
	public Transform RHandIKAttachmentPoint;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private AudioSource rollingAudioSource;

	private float _originalMovingTurnSpeed;

	private float _originalRotationSpeed;

	private float _originalStationaryTurnSpeed;

	private float _originalTurningSpeed;

	public override void Awake()
	{
		base.Awake();
		showCargoInItemPanel = true;
		rollingAudioSource.outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.playerVehicleMixerGroup;
		_originalMovingTurnSpeed = InstanceBehavior<GameManager>.Instance.playerController.Character.m_MovingTurnSpeed;
		_originalStationaryTurnSpeed = InstanceBehavior<GameManager>.Instance.playerController.Character.m_StationaryTurnSpeed;
		_originalRotationSpeed = InstanceBehavior<GameManager>.Instance.playerController.Character.m_RotationSpeed;
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
	}

	protected override void Update()
	{
		base.Update();
		if (controlledByPlayer)
		{
			ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
			float num = character.CurrentVelocity / character.navmeshAgent.speed;
			rollingAudioSource.volume = (minVolume + (maxVolume - minVolume) * num) * mainVolume;
			float b = InstanceBehavior<GameManager>.Instance.playerController.Character.walkingSpeed switch
			{
				ThirdPersonCharacter.WalkingSpeed.Zombie => zombiePitch, 
				ThirdPersonCharacter.WalkingSpeed.Run => runningPitch, 
				_ => walkingPitch, 
			};
			rollingAudioSource.pitch = Mathf.Lerp(rollingAudioSource.pitch, b, Time.deltaTime * pitchFadeSpeed);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
	}

	private void OnPause(bool paused)
	{
		if (paused)
		{
			rollingAudioSource?.Pause();
		}
		else
		{
			rollingAudioSource?.UnPause();
		}
	}

	public override bool Interact()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.isActiveAndEnabled)
		{
			return false;
		}
		if ((PlayerHelper.ItemInstanceInHands != null && PlayerHelper.IsHoldingAMop) || PlayerHelper.IsHoldingShoppingBasket)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "fromname", "vehicletypename_handtruck" },
				{
					"toname",
					PlayerHelper.ItemInstanceInHands.itemName
				}
			};
			Notifications.Show(NotificationType.Warning, "notification_cant_use_while_using_item", notificationData);
			return true;
		}
		bool flag = base.Interact();
		if (PlayerHelper.ItemInstanceInHands == null && SaveGameManager.Current.ActiveVehicleId == null && !CasinoBoatManager.boatSailInSequenceStarted && !flag)
		{
			flag = true;
			EnterVehicle();
		}
		return flag;
	}

	public void Release()
	{
		ExitVehicle();
	}

	private void UpdateCardboardBoxes()
	{
		int num = vehicleInstance.cargoInstances?.Count ?? 0;
		for (int i = 0; i < boxPlaceholder.childCount; i++)
		{
			boxPlaceholder.GetChild(i).gameObject.SetActive(num > i);
		}
	}

	public void SetAnimSpeed(float speed)
	{
		if (animator != null)
		{
			animator.SetFloat(MoveSpeed, speed);
		}
	}

	public override void EnterVehicle()
	{
		base.EnterVehicle();
		rollingAudioSource.Play();
		navMeshObstacle.enabled = false;
		base.transform.SetParent(InstanceBehavior<GameManager>.Instance.playerController.transform);
		base.transform.localRotation = Quaternion.Euler(base.transform.eulerAngles.x, 0f, base.transform.eulerAngles.z);
		base.transform.localPosition = Vector3.zero;
		vehicleCollider.enabled = false;
		Transform transform = PlayerHelper.GetAnimator().transform;
		ThirdPersonCharacter playerTpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		if (transform.localPosition == Vector3.zero)
		{
			playerTpc.navmeshAgent.enabled = false;
			CoroutineUtility.RunAfterFrameDelay(delegate
			{
				playerTpc.navmeshAgent.enabled = true;
			}, 3);
			transform.parent.position -= transform.parent.forward * playerOffsetWhenActive;
			Vector3 localPosition = transform.localPosition;
			localPosition.z += playerOffsetWhenActive;
			transform.localPosition = localPosition;
		}
		if (animator != null)
		{
			animator.enabled = true;
			SetAnimSpeed(0f);
		}
		playerTpc.m_MovingTurnSpeed = 1f;
		playerTpc.m_StationaryTurnSpeed = 1f;
		playerTpc.m_RotationSpeed = 1f;
		string text = InstanceBehavior<BuildingManager>.Instance?.building?.StreetName ?? string.Empty;
		int valueOrDefault = (InstanceBehavior<BuildingManager>.Instance?.building?.StreetNumber).GetValueOrDefault();
		vehicleInstance.SetStreetData(text, valueOrDefault);
		playerTpc.SetHandIKTargets(LHandIKAttachmentPoint, RHandIKAttachmentPoint);
		PlayerHelper.GetAnimator().SetBool(UsingHands, value: true);
	}

	public override void ExitVehicle()
	{
		base.transform.SetParent(InstanceBehavior<GameManager>.Instance.itemsContainer);
		if (this.animator != null)
		{
			this.animator.enabled = false;
			base.transform.localRotation = Quaternion.Euler(0f, base.transform.eulerAngles.y, base.transform.eulerAngles.z);
		}
		navMeshObstacle.enabled = true;
		vehicleCollider.enabled = true;
		Animator animator = PlayerHelper.GetAnimator();
		animator.transform.parent.position = animator.transform.position;
		animator.transform.localPosition = Vector3.zero;
		animator.SetBool(UsingHands, value: false);
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		character.m_MovingTurnSpeed = _originalMovingTurnSpeed;
		character.m_StationaryTurnSpeed = _originalStationaryTurnSpeed;
		character.m_RotationSpeed = _originalRotationSpeed;
		if (character.navmeshAgent.enabled)
		{
			character.navmeshAgent.ResetPath();
		}
		else
		{
			character.navmeshAgent.enabled = true;
		}
		character.SetHandIKTargets(null, null);
		base.ExitVehicle();
		rollingAudioSource.Stop();
		rollingAudioSource.volume = 0f;
	}

	public override void UpdateCargoCount()
	{
		base.UpdateCargoCount();
		UpdateCardboardBoxes();
	}

	protected override Vector3 GetDrivingEntrancePosition()
	{
		return GetClosestNavMeshTargetPosition(InstanceBehavior<GameManager>.Instance.playerController.transform.position);
	}
}
