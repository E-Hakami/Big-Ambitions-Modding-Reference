using BigAmbitions.SoundSystem;
using UnityEngine;

public class StepTrigger : MonoBehaviour
{
	private const float ZombieWalkPitchChange = -0.3f;

	public bool ignore;

	public Transform[] footSoundPosition;

	public float minSeparation = 0.2f;

	private bool _isPlayer;

	private float _lastTimeExecuted = -999f;

	private void Start()
	{
		if ((bool)InstanceBehavior<GameManager>.Instance)
		{
			_isPlayer = base.transform.IsChildOf(InstanceBehavior<GameManager>.Instance.playerController.transform);
		}
	}

	public void TriggerStepSound(AnimationEvent footstepEvent)
	{
		if (ignore || footstepEvent.animatorClipInfo.weight <= 0.5f || !InstanceBehavior<SfxManager>.Instance || Time.time < _lastTimeExecuted)
		{
			return;
		}
		_lastTimeExecuted = Time.time + minSeparation;
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		float num = 0f;
		num = Vector3.SqrMagnitude(base.transform.position - playerController.transform.position);
		if (!(num >= 100f))
		{
			num = Mathf.Sqrt(num);
			SoundType type = SoundType.FootstepAsphalt;
			int intParameter = footstepEvent.intParameter;
			Vector3 position = footSoundPosition[intParameter].position;
			if (Physics.Raycast(new Ray(position + Vector3.up * 0.2f, Vector3.down), out var hitInfo, 2f, LayerMask.GetMask("Ground")) && hitInfo.transform.TryGetComponent<StepSound>(out var component))
			{
				type = (SoundType)component.footStepSoundType;
			}
			if (_isPlayer)
			{
				float addPitch = ((playerController.Character.walkingSpeed == ThirdPersonCharacter.WalkingSpeed.Zombie) ? (-0.3f) : 0f);
				InstanceBehavior<SfxManager>.Instance.PlayAudio(type, footSoundPosition[intParameter].position, 0.8f, isPlayerCreatedSound: true, InstanceBehavior<SfxManager>.Instance.playerFootStepAudioMixerGroup, -1f, addPitch);
			}
			else
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(type, footSoundPosition[intParameter].position, 0.2f + (10f - num) / 8f);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Transform[] array = footSoundPosition;
		foreach (Transform transform in array)
		{
			Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
			if (Physics.Raycast(ray, out var hitInfo, 2f, LayerMask.GetMask("Ground")) && hitInfo.transform.TryGetComponent<StepSound>(out var _))
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(ray.origin, hitInfo.point);
			}
			else
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(ray.origin, ray.GetPoint(2f));
			}
		}
	}
}
