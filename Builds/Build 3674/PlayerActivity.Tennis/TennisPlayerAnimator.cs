using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisPlayerAnimator : MonoBehaviour
{
	[SerializeField]
	private TennisPlayer player;

	private void OnAnimatorIK(int layerIndex)
	{
		player.ApplyAnimatorIK(layerIndex);
	}

	public void OnBallServe()
	{
		player.OnBallServe();
	}
}
