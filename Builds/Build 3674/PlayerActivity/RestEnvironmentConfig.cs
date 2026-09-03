using BigAmbitions.Characters;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "RestEnvironmentConfig", menuName = "BigAmbitions/PlayerActivity/RestEnvironmentConfig")]
public class RestEnvironmentConfig : PlayerActivityEnergyEnvironmentConfig
{
	public RestEnvironmentType environmentType;

	public PlayerActivityBalanceConfig watchShowBalanceConfig;

	[Tooltip("If set, picks a random animation from this list when the player watches nearby entertainment instead of the default sitting animation.")]
	[SearchableEnum]
	public PermanentAnimationType[] entertainAnimationOverride;
}
