using System;

namespace BusinessLayoutSets;

[Serializable]
public class FactoryItem : Item
{
	public string selectedRecipeId;

	public string workstationType;

	public int priority;

	public bool produceUpTo;

	public int produceUpToValue;
}
