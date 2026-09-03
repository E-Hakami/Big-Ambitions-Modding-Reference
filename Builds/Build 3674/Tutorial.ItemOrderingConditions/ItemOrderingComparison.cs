using UnityEngine;

namespace Tutorial.ItemOrderingConditions;

public abstract class ItemOrderingComparison : ScriptableObject
{
	public abstract int Comparison(string a, string b);
}
