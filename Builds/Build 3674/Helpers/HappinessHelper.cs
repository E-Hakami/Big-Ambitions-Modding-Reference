using System.Collections.Generic;
using System.Linq;
using HGAttributes;
using IngameDebugConsole;
using UI;
using UnityEngine;

namespace Helpers;

public static class HappinessHelper
{
	public const string AddressableLabel = "HappinessModifiers";

	private static Dictionary<string, HappinessModifier> Modifiers;

	[AutocompleteProvider("HappinessModifiers")]
	private static IEnumerable<string> ModifierNames => Modifiers.Keys;

	public static float Happiness
	{
		get
		{
			return SaveGameManager.Current.Happiness;
		}
		private set
		{
			SaveGameManager.Current.Happiness = value;
		}
	}

	private static List<HappinessModifierData> HappinessModifiers
	{
		get
		{
			GameInstance current = SaveGameManager.Current;
			if (current.happinessModifiers == null)
			{
				current.happinessModifiers = new List<HappinessModifierData>();
			}
			return SaveGameManager.Current.happinessModifiers;
		}
	}

	private static List<string> UsedHappinessModifiers => SaveGameManager.Current.usedHappinessModifiers;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Modifiers = null;
	}

	public static void OnHappinessModifiersLoaded(IList<HappinessModifier> modifiers)
	{
		Modifiers = new Dictionary<string, HappinessModifier>(modifiers.Count);
		foreach (HappinessModifier modifier in modifiers)
		{
			Modifiers.Add(modifier.type, modifier);
		}
	}

	public static void RunHourly()
	{
		if (!SaveGameManager.Current.gameVariables.disableHappiness)
		{
			UpdateHappinessModifiers();
			UpdateHappiness();
		}
	}

	private static void UpdateHappinessModifiers()
	{
		List<HappinessModifierData> finishedModifiers = new List<HappinessModifierData>();
		foreach (HappinessModifierData happinessModifier in HappinessModifiers)
		{
			if (happinessModifier == null)
			{
				continue;
			}
			if (happinessModifier.hoursLeft == -1)
			{
				if (Modifiers.TryGetValue(happinessModifier.type, out var value) && value.hoursDuration != -1)
				{
					finishedModifiers.Add(happinessModifier);
				}
				continue;
			}
			happinessModifier.hoursLeft--;
			if (happinessModifier.hoursLeft <= 0)
			{
				finishedModifiers.Add(happinessModifier);
			}
		}
		HappinessModifiers.RemoveAll((HappinessModifierData x) => finishedModifiers.Contains(x));
	}

	public static void UpdateHappiness()
	{
		float happiness = Happiness;
		int num = 0;
		foreach (HappinessModifierData happinessModifier3 in HappinessModifiers)
		{
			if (happinessModifier3 == null)
			{
				Debug.LogError("Null happiness modifier data");
				continue;
			}
			foreach (var (text2, happinessModifier2) in Modifiers)
			{
				if (!(text2 != happinessModifier3.type))
				{
					num += happinessModifier2.amount;
					break;
				}
			}
		}
		Happiness = Mathf.Clamp(num, 0, 100);
		ShowHappinessEmojis(happiness);
	}

	private static void ShowHappinessEmojis(float oldHappinessValue)
	{
		if (Happiness < 5f && oldHappinessValue >= 5f)
		{
			ShowPlayerExpression(CharacterEmojiName.PlayerHappinessTooLow);
		}
		else if (Happiness < 25f && oldHappinessValue >= 25f)
		{
			ShowPlayerExpression(CharacterEmojiName.PlayerHappinessLow);
		}
	}

	private static void ShowPlayerExpression(CharacterEmojiName characterEmojiName)
	{
		(InstanceBehavior<GameManager>.Instance?.playerController.Character)?.EnqueuePlayerExpression(characterEmojiName, 2f);
	}

	private static bool IsModifierActive(string type)
	{
		return HappinessModifiers.Any((HappinessModifierData x) => x.type == type);
	}

	public static HappinessModifier GetHappinessModifierFromType(string requestedType)
	{
		if (Modifiers.TryGetValue(requestedType, out var value))
		{
			return value;
		}
		Debug.LogError("No happiness modifier found with ID " + requestedType);
		return null;
	}

	public static int GetCappedHoursDuration(string type, int hoursDuration)
	{
		if (!Modifiers.TryGetValue(type, out var value))
		{
			Debug.LogError("No happiness modifier found with ID " + type);
			return hoursDuration;
		}
		return GetCappedHoursDuration(value, hoursDuration);
	}

	public static void AddModifier(string type, int customHoursDuration = -1, bool additiveHours = false)
	{
		if (SaveGameManager.Current.gameVariables.disableHappiness)
		{
			return;
		}
		HappinessModifier happinessModifier = Modifiers[type];
		int num = ((customHoursDuration == -1) ? happinessModifier.hoursDuration : customHoursDuration);
		if (num == 0)
		{
			return;
		}
		if (happinessModifier.oneTimeOnly)
		{
			if (UsedHappinessModifiers.Contains(type))
			{
				return;
			}
			UsedHappinessModifiers.Add(type);
		}
		else
		{
			if (happinessModifier.amount > 0 && InstanceBehavior<UIs>.Instance != null && !InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerHappinessIncrease);
			}
			HappinessModifierData happinessModifierData = HappinessModifiers.FirstOrDefault((HappinessModifierData x) => x.type == type);
			if (happinessModifierData != null)
			{
				if (additiveHours)
				{
					happinessModifierData.hoursLeft = GetCappedHoursDuration(happinessModifier, happinessModifierData.hoursLeft, num);
				}
				else
				{
					happinessModifierData.hoursLeft = GetCappedHoursDuration(happinessModifier, num);
				}
				return;
			}
		}
		HappinessModifierData item = new HappinessModifierData
		{
			type = type,
			hoursLeft = GetCappedHoursDuration(happinessModifier, num),
			hideDuration = happinessModifier.hideDuration
		};
		HappinessModifiers.Add(item);
		UpdateHappiness();
		if (InstanceBehavior<UIs>.Instance != null)
		{
			GameEvent.Invoke(string.Empty);
		}
	}

	public static void RemoveModifier(string type)
	{
		if (!SaveGameManager.Current.gameVariables.disableHappiness)
		{
			HappinessModifiers.RemoveAll((HappinessModifierData x) => x.type == type);
			UpdateHappiness();
		}
	}

	[ConsoleMethod("addHappinessModifier", "Add happiness modifier to player", new string[] { }, AutoCompleteMap = new string[] { "modifier=HappinessModifiers" })]
	public static void Command_AddHappinessModifier(string modifier)
	{
		AddModifier(modifier);
	}

	public static void EnableTemporalHappinessBoost(string temporalBoost, string regularBoost, ThirdPersonCharacter tpc = null)
	{
		if ((object)tpc == null)
		{
			tpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		}
		tpc.EnableHappinessBoostEmojiShower();
		if (!IsModifierActive(regularBoost))
		{
			AddModifier(temporalBoost);
		}
	}

	public static void DisableTemporalHappinessBoost(string temporalBoost, string regularBoost, ThirdPersonCharacter tpc = null)
	{
		if ((object)tpc == null)
		{
			tpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		}
		tpc.DisableHappinessBoostEmojiShower();
		int hoursOfHappinessBoost = GetHoursOfHappinessBoost();
		if (hoursOfHappinessBoost > 0)
		{
			AddModifier(regularBoost, hoursOfHappinessBoost, additiveHours: true);
		}
		RemoveModifier(temporalBoost);
	}

	public static void ConvertTemporalBoostsToRegularBoosts()
	{
		for (int num = HappinessModifiers.Count - 1; num >= 0; num--)
		{
			HappinessModifierData happinessModifierData = HappinessModifiers[num];
			string nonTemporalType = GetHappinessModifierFromType(happinessModifierData.type).nonTemporalType;
			if (happinessModifierData.hoursLeft == -1 && !(nonTemporalType == "ba:happinessmodifier_cheat"))
			{
				int hoursOfHappinessBoost = GetHoursOfHappinessBoost();
				if (hoursOfHappinessBoost > 0)
				{
					AddModifier(nonTemporalType, hoursOfHappinessBoost, additiveHours: true);
				}
				HappinessModifiers.RemoveAt(num);
			}
		}
	}

	private static int GetHoursOfHappinessBoost()
	{
		float num = (TimeHelper.NowInMinutes() - SaveGameManager.Current.timeEnteredTemporalBoost.GetTotalMinutes()) / 60f;
		return Mathf.FloorToInt(SaveGameManager.Current.currentActivityHappinessPerHour * num);
	}

	private static int GetCappedHoursDuration(HappinessModifier modifier, int hoursDuration)
	{
		if (hoursDuration < 0 || modifier.maxHoursDuration <= 0)
		{
			return hoursDuration;
		}
		return Mathf.Min(hoursDuration, modifier.maxHoursDuration);
	}

	private static int GetCappedHoursDuration(HappinessModifier modifier, int currentHours, int hoursToAdd)
	{
		if (currentHours < 0 || hoursToAdd < 0 || modifier.maxHoursDuration <= 0)
		{
			return currentHours + hoursToAdd;
		}
		return Mathf.Min(currentHours + hoursToAdd, modifier.maxHoursDuration);
	}
}
