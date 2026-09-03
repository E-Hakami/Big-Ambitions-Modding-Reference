using UnityEngine;

namespace Buildings.Office.Headquarters;

[CreateAssetMenu(fileName = "PricingManagerSettings", menuName = "BigAmbitions/PricingManagerSettings")]
public class PricingManagerSettings : ScriptableObject
{
	[Header("Suggested Price")]
	[Tooltip("How close a 0% skill analyst gets to the best price. At 0 they just suggest the market price. At 1 even a bad analyst hits the best price. A 100% skill analyst always does.")]
	[Range(0f, 1f)]
	public float minSkillCapture;

	[Tooltip("How much each product can randomly land lower than the analyst's skill would give. This stops a weak analyst from being off by the same amount on everything. It goes away at 100% skill. Each product keeps its roll until you assign someone else.")]
	[Range(0f, 0.5f)]
	public float captureJitter = 0.15f;

	[Tooltip("How wide the price range in the app is. At 0 the analyst shows one price instead of a range. It goes away at 100% skill.")]
	[Range(0f, 0.5f)]
	public float visibleSpread;

	[Header("Update Schedule")]
	[Tooltip("Hour of the day the analyst starts working, 0 to 23.")]
	[Range(0f, 23f)]
	public int updateHour = 8;

	[Tooltip("Game hours between updates. At 24 the analyst works once a day at the update hour. Lower keeps up with the market better, but recalculates more often.")]
	[Range(1f, 24f)]
	public int hoursBetweenUpdates = 24;

	[Header("Apply Suggested Prices Button")]
	[Tooltip("When on, the button only reprices products the active filter leaves visible.")]
	public bool applyOnlyToVisibleProducts = true;
}
