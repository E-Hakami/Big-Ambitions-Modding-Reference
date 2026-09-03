using BigAmbitions.Characters;
using UnityEngine;

public class CharacterRunAnimation
{
	private ThirdPersonCharacter _tpc;

	private float _duration;

	private float _startTime;

	public void Init(ThirdPersonCharacter tpc)
	{
		_tpc = tpc;
	}

	public void StartRunningAnimation(AnimationType animationType, float animationSpeed = 1f)
	{
		_duration = _tpc.animator.RunAnimationLength(animationType, animationSpeed);
		_startTime = Time.time;
	}

	public bool IsAnimationFinished()
	{
		return _startTime + _duration < Time.time;
	}
}
