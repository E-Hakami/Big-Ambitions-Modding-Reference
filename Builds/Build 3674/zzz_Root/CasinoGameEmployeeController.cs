using System.Collections;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using Buildings.BuildingTypes.Special;
using Character;
using RoboRyanTron.SearchableEnum;
using UnityEngine;
using UnityEngine.AI;

public class CasinoGameEmployeeController : MonoBehaviour
{
	[SerializeField]
	protected PlaySpotsManager playSpotsManager;

	[SerializeField]
	[SearchableEnum]
	protected PermanentAnimationType idleAnimation;

	[SerializeField]
	[SearchableEnum]
	protected AnimationType dealAnimation;

	[SerializeField]
	[SearchableEnum]
	protected SoundType dealerSound;

	[SerializeField]
	protected float timeBetweenAnimations = 5f;

	[SerializeField]
	[Range(0f, 1f)]
	protected float soundVolume = 1f;

	protected ThirdPersonCharacter employeeTpc;

	protected bool isDealerActive;

	protected float timer;

	public virtual void SetEmployeeTpc(ThirdPersonCharacter tpc)
	{
		employeeTpc = tpc;
		PlayIdleAnimation();
		employeeTpc.animator.GetComponent<AnimationTriggerEvents>().oneActionTrigger.AddListener(OnAnimationTrigger);
		employeeTpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
	}

	private void Update()
	{
		bool flag = playSpotsManager.IsAnySpotOccupied() && !playSpotsManager.IsPlayerPlaying();
		if (flag && isDealerActive)
		{
			if (timer >= timeBetweenAnimations)
			{
				PlayDealAnimation();
				timer = 0f;
			}
			else
			{
				timer += Time.deltaTime;
			}
		}
		else
		{
			isDealerActive = flag;
			timer = 0f;
		}
	}

	private void PlayIdleAnimation()
	{
		employeeTpc.animator.SetBool(idleAnimation);
	}

	private void PlayDealAnimation()
	{
		employeeTpc.animator.SetTrigger(dealAnimation);
	}

	public void PlayDealAnimationDelayed(float delay)
	{
		StartCoroutine(PlayDealAnimationDelayedCoroutine(delay));
	}

	private IEnumerator PlayDealAnimationDelayedCoroutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		PlayDealAnimation();
	}

	protected virtual void OnAnimationTrigger()
	{
		if (!playSpotsManager.IsPlayerPlaying())
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(dealerSound, base.transform.position, soundVolume);
		}
	}
}
