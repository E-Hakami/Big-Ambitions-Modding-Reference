using UnityEngine;

public class RouletteEmployeeController : CasinoGameEmployeeController
{
	private static readonly int RouletteSpin = Animator.StringToHash("RouletteSpin");

	[SerializeField]
	private Animator rouletteAnimator;

	protected override void OnAnimationTrigger()
	{
		base.OnAnimationTrigger();
		PlayRouletteAnimation();
	}

	private void PlayRouletteAnimation()
	{
		rouletteAnimator.SetTrigger(RouletteSpin);
	}
}
