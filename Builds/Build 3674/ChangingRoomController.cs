using UnityEngine;

public class ChangingRoomController : ItemController
{
	private static readonly int OpenCurtain = Animator.StringToHash("OpenCurtain");

	private static readonly int CloseCurtain = Animator.StringToHash("CloseCurtain");

	public Transform npcSpot;

	[SerializeField]
	private Animator animator;

	public void OpenCurtains()
	{
		animator.SetTrigger(OpenCurtain);
		animator.ResetTrigger(CloseCurtain);
	}

	public void CloseCurtains()
	{
		animator.SetTrigger(CloseCurtain);
		animator.ResetTrigger(OpenCurtain);
	}
}
