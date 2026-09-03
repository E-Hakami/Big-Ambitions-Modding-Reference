using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "SleepEnvironmentConfig", menuName = "BigAmbitions/PlayerActivity/SleepEnvironmentConfig")]
public class SleepEnvironmentConfig : PlayerActivityEnergyEnvironmentConfig
{
	public SleepEnvironmentType sleepEnvironmentType;

	public PlayerActivityBalanceConfig luxuryOverrideBalanceConfig;
}
