using BigAmbitions.Characters;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "StreetPerformerData", menuName = "BigAmbitions/StreetPerformerData")]
public class StreetPerformerData : ScriptableObject
{
	[SearchableEnum]
	public PermanentAnimationType animation;

	public PerformerObjectData[] objectsData;

	public PerformerObjectData[] spawnOnlyOneObjectsData;

	[Header("SFX")]
	public AudioClip clip;

	public AudioClip clipFemaleVariant;
}
