using System.Collections;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

public class BlackjackEmployeeController : CasinoGameEmployeeController
{
	[SerializeField]
	[SearchableEnum]
	private SoundType dealerSecondSound;

	private readonly WaitForSeconds waitForSeconds = new WaitForSeconds(0.95f);

	public override void SetEmployeeTpc(ThirdPersonCharacter tpc)
	{
		base.SetEmployeeTpc(tpc);
		string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType.DealerBlackjackIdle);
		employeeTpc.AddHandObject(handObjectNameFromPermanentAnimationType, isRightHand: false);
	}

	protected override void OnAnimationTrigger()
	{
		base.OnAnimationTrigger();
		if (isDealerActive)
		{
			StartCoroutine(PlayCardSounds());
		}
	}

	private IEnumerator PlayCardSounds()
	{
		for (int i = 0; i < 2; i++)
		{
			yield return waitForSeconds;
			base.OnAnimationTrigger();
		}
		timer = 0f;
	}
}
