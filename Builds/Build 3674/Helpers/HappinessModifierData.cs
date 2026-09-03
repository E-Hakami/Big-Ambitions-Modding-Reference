using System;
using UnityEngine.Serialization;

namespace Helpers;

[Serializable]
public class HappinessModifierData
{
	public string type;

	public int hoursLeft;

	[FormerlySerializedAs("showDuration")]
	public bool hideDuration;
}
