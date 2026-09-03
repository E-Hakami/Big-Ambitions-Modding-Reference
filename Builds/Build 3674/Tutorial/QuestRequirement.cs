using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial;

public abstract class QuestRequirement : ScriptableObject
{
	public virtual List<string> ChangesToCheckOn => new List<string>();

	public abstract bool CheckIfCompleted();

	public virtual bool CheckIfCompleted(string changeType)
	{
		return CheckIfCompleted();
	}

	protected static bool ItemMatches(string itemName, string[] itemNames, string[] itemTags)
	{
		for (int i = 0; i < itemNames.Length; i++)
		{
			if (itemNames[i] == itemName)
			{
				return true;
			}
		}
		if (itemTags == null || itemTags.Length == 0)
		{
			return false;
		}
		Item byName = ItemsGetter.GetByName(itemName, suppressError: true);
		if (byName == null)
		{
			return false;
		}
		for (int j = 0; j < itemTags.Length; j++)
		{
			if (byName.HasTag(itemTags[j]))
			{
				return true;
			}
		}
		return false;
	}
}
