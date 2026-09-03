using AI;
using JimmysUnityUtilities;
using UnityEngine;

public class GolferPool : BaseHumanPool
{
	[Header("Golfer Pool")]
	public float minSwingDelay = 2f;

	public float maxSwingDelay = 5f;

	public float swingDuration = 6f;

	public GameObject clubPrefab;

	public Vector3 clubHeldPosition;

	public Vector3 clubHeldRotation;

	public AudioClip shotSound;

	public float shotSoundVolume = 1f;

	public float shotSoundPitch = 1f;

	public float shotSoundPitchVariation = 0.1f;

	protected override void InitHuman(BaseHuman human)
	{
		base.InitHuman(human);
		GolferNpc golferNpc = human.AddComponent<GolferNpc>();
		golferNpc.pool = this;
		golferNpc.animator = human.animator;
	}
}
