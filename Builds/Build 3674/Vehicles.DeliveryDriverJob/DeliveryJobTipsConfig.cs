using UnityEngine;

namespace Vehicles.DeliveryDriverJob;

[CreateAssetMenu(fileName = "DeliveryJobTipsConfig", menuName = "BigAmbitions/DeliveryJob/TipsConfig")]
public class DeliveryJobTipsConfig : ScriptableObject
{
	public TipChance[] tipChances;

	public float fastDeliveryTimeRatio = 0.5f;

	public float fastDeliveryChanceUp = 0.05f;

	public bool IsFastDelivery(float minutesUsed, int timeLimitMinutes)
	{
		return minutesUsed <= (float)timeLimitMinutes * fastDeliveryTimeRatio;
	}

	public float RollTip(bool wasFastDelivery)
	{
		if (tipChances == null || tipChances.Length == 0)
		{
			return 0f;
		}
		float value = Random.value;
		float num = 0f;
		TipChance[] array = tipChances;
		foreach (TipChance tipChance in array)
		{
			float num2 = tipChance.chance;
			if (wasFastDelivery)
			{
				num2 += fastDeliveryChanceUp;
			}
			if (tipChance.tip > num && value < num2)
			{
				num = tipChance.tip;
			}
		}
		return num;
	}
}
