using System;
using BigAmbitions.Characters;
using RoboRyanTron.SearchableEnum;

namespace Entities;

[Serializable]
public class CombinedAnimation
{
	[SearchableEnum]
	public PermanentAnimationType permanentAnimation;

	[SearchableEnum]
	public AnimationType animation;
}
