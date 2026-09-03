using UnityEngine;

namespace Characters;

public class BoredAnimations : MonoBehaviour
{
	private static readonly int BoredId = Animator.StringToHash("Bored");

	private static readonly int BoredAnimationId = Animator.StringToHash("BoredAnimation");

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private int numberOfAnimations;

	[SerializeField]
	private float minTimeToTrigger;

	[SerializeField]
	private float maxTimeToTrigger;

	[HideInInspector]
	public bool pauseBoredAnimations;

	[HideInInspector]
	public bool isBoredAnimationPausedSetting;

	private float _timeForNextTrigger;

	private void OnEnable()
	{
		if (animator == null)
		{
			Debug.LogError("No animator found for " + base.name);
			base.enabled = false;
		}
		else if (numberOfAnimations == 0)
		{
			Debug.LogError("Number of bored animations for " + base.name + " is 0");
			base.enabled = false;
		}
		else
		{
			SetTimeForNextTrigger();
		}
	}

	private void Update()
	{
		if (ShouldTriggerAnimation())
		{
			TriggerRandomAnimation();
			SetTimeForNextTrigger();
		}
	}

	private bool ShouldTriggerAnimation()
	{
		return _timeForNextTrigger <= Time.time;
	}

	private void TriggerRandomAnimation()
	{
		if (!pauseBoredAnimations && !isBoredAnimationPausedSetting)
		{
			animator.SetFloat(BoredAnimationId, Random.Range(0, numberOfAnimations));
			animator.SetTrigger(BoredId);
		}
	}

	private void SetTimeForNextTrigger()
	{
		_timeForNextTrigger = Time.time + Random.Range(minTimeToTrigger, maxTimeToTrigger);
	}
}
