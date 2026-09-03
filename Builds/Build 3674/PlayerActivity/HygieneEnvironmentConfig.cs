using BigAmbitions.Characters;
using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "HygieneEnvironmentConfig", menuName = "BigAmbitions/PlayerActivity/HygieneEnvironmentConfig")]
public class HygieneEnvironmentConfig : PlayerActivityEnvironmentConfig
{
	public PlayerActivityBalanceConfig balanceConfig;

	public bool isFixedDuration;

	public PermanentAnimationType animationType = PermanentAnimationType.Sitting;

	public string uiLabelKey;

	public string uiStartKey;
}
