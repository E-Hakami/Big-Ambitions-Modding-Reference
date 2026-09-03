using System;
using Character;
using Items.SpecialItems;
using UnityEngine;

namespace AI;

public class GolferNpc : MonoBehaviour
{
	private static readonly int GolfSwing = Animator.StringToHash("GolfSwing");

	[NonSerialized]
	public GolferPool pool;

	[NonSerialized]
	public Animator animator;

	[NonSerialized]
	public bool manualControl;

	private float _swingTimer;

	private Transform _hand;

	private GameObject _club;

	private void Start()
	{
		ResetSwingTimer();
		_hand = base.transform.Find("Model/Armature/Hips/Spine/Chest/UpperChest/Shoulder.R/UpperArm.R/LowerArm.R/Hand.R");
		if (!_hand)
		{
			Debug.LogError("Hand not found for GolferNpc");
			return;
		}
		_club = UnityEngine.Object.Instantiate(pool.clubPrefab, _hand);
		_club.transform.localPosition = pool.clubHeldPosition;
		_club.transform.localEulerAngles = pool.clubHeldRotation;
		if (!manualControl)
		{
			animator.GetComponent<AnimationTriggerEvents>().oneActionTrigger.AddListener(OnAnimationTrigger);
		}
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			if ((bool)_club)
			{
				UnityEngine.Object.Destroy(_club);
			}
			if (!manualControl)
			{
				animator.GetComponent<AnimationTriggerEvents>().oneActionTrigger.RemoveListener(OnAnimationTrigger);
			}
		}
	}

	private void Update()
	{
		if (!manualControl)
		{
			_swingTimer -= Time.deltaTime;
			if (_swingTimer <= 0f)
			{
				animator.SetTrigger(GolfSwing);
				ResetSwingTimer();
				_swingTimer += pool.swingDuration;
			}
		}
	}

	private void ResetSwingTimer()
	{
		_swingTimer = UnityEngine.Random.Range(pool.minSwingDelay, pool.maxSwingDelay);
	}

	private void OnAnimationTrigger()
	{
		if (!GolfPlatformController.PlayingInstance)
		{
			float pitch = pool.shotSoundPitch + pool.shotSoundPitchVariation * UnityEngine.Random.Range(-1f, 1f);
			InstanceBehavior<SfxManager>.Instance.PlayAudio(pool.shotSound, base.transform.position, pool.shotSoundVolume, pitch, 1f, isPlayerCreatedSound: false, InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup);
		}
	}
}
