using UnityEngine;
using UnityEngine.Playables;

namespace Controllers;

public class NpcSpawnerItemWithTimeline : NpcSpawnerItem
{
	[SerializeField]
	private PlayableDirector playableDirector;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	[Tooltip("Name of the track in the timeline that binds to the human animator.")]
	private string humanTrackAssetName;

	public override void OnNpcSpawn(BaseHuman baseHuman)
	{
		base.OnNpcSpawn(baseHuman);
		InitAnimation(baseHuman);
	}

	private void InitAnimation(BaseHuman baseHuman)
	{
		animator.enabled = true;
		playableDirector.SetBindingOnTimelineFromTrackAssetName(baseHuman.animator, humanTrackAssetName);
		playableDirector.Play();
		playableDirector.time = Random.Range(0f, (float)playableDirector.duration);
	}

	public override void OnNpcDespawn()
	{
		base.OnNpcDespawn();
		playableDirector.time = 0.0;
		playableDirector.Evaluate();
		playableDirector.Stop();
		animator.enabled = false;
	}
}
