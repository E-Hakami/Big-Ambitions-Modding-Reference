using System.Collections.Generic;

namespace Buildings.Office.Headquarters;

public class PriceSuggestion
{
	public string itemName;

	public float suggestedMin;

	public float suggestedMax;

	public float rivalReferencePrice;

	public bool isPlayerSelling;

	public HashSet<string> sellingBusinessTypes;
}
