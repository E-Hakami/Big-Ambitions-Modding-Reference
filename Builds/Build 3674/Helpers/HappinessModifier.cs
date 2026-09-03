using UnityEngine;

namespace Helpers;

[CreateAssetMenu(fileName = "HappinessModifier", menuName = "BigAmbitions/HappinessModifier", order = 0)]
public class HappinessModifier : ScriptableObject
{
	[Tooltip("Type of the modifier. e.g. PlayedVideogames")]
	public string type;

	[Tooltip("Number of hours the modifier lasts. -1 for infinite (until turned off)")]
	public int hoursDuration;

	[Tooltip("Maximum number of hours this modifier can have active at once. 0 or less means uncapped")]
	public int maxHoursDuration = -1;

	[Tooltip("Amount of happiness it provides in happiness percentage. Between -100 and 100")]
	[Range(-100f, 100f)]
	public int amount;

	[Tooltip("If true, the modifier can only be applied once. e.g. FirstJob")]
	public bool oneTimeOnly;

	[Tooltip("If true, the UI won't show the duration of the modifier. Useful if the modifier is infinite")]
	public bool hideDuration;

	[Tooltip("Optional: Non-temporal type of the modifier. e.g. PlayingVideogames")]
	public string nonTemporalType;
}
