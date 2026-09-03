using System;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;

namespace Entities;

[Serializable]
public class StationaryAiData
{
	public AppearanceTag[] appearanceTags;

	public bool usePermanentAnim;

	[ShowIf("usePermanentAnim")]
	[SearchableEnum]
	[AllowNesting]
	public PermanentAnimationType[] permanentAnims;

	[HideIf("usePermanentAnim")]
	[SearchableEnum]
	[AllowNesting]
	public AnimationType[] animations;

	[ShowIf("usePermanentAnim")]
	[AllowNesting]
	public CombinedAnimation[] combinedAnimations;

	[HideIf("usePermanentAnim")]
	[AllowNesting]
	public float minTimeBetweenAnims;

	[HideIf("usePermanentAnim")]
	[AllowNesting]
	public float maxTimeBetweenAnims;

	public bool skipMeshMerging;
}
