using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest")]
public class Quest : ScriptableObject
{
	public int order;

	public string uncleFredMessageType;

	public QuestCheckpointData checkpointData;

	public AudioClip uncleFredAudioClip;

	public QuestAction[] onStartActions;

	public QuestEntry[] entries;

	public string Id => base.name;
}
