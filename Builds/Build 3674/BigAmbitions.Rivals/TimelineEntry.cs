using System;
using System.Linq;
using Enums;
using Extensions;
using UnityEngine;

namespace BigAmbitions.Rivals;

[Serializable]
public class TimelineEntry
{
	public string id = UuidHelper.GenerateBase64Uuid();

	[Header("Triggers")]
	public int businesses;

	public int weeklyIncomePercentage;

	[Header("Behavior")]
	public DefensiveMechanic defense;

	public Priority aggression;

	[Header("Message")]
	public string messageLocalizationKey;

	public AudioClip messageClip;

	public bool IsCompleted => SaveGameManager.Current.specialRivalStates.Any((SpecialRivalState x) => x.completedTimelineEntryIds.Contains(id));
}
