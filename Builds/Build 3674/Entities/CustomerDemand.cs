using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Entities;

public abstract class CustomerDemand : ScriptableObject
{
	public string demandType;

	public CharacterEmojiName characterEmojiName;

	public abstract bool Fulfilled(BuildingRegistration registration, HashSet<Item> items);
}
