using Buildings.Office.Headquarters;
using Localizor;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerProductModel
{
	private const float MissingValueSortKey = -1f;

	public PriceSuggestion Suggestion { get; }

	public PricingManagerPlan Plan { get; }

	public string ProductName { get; }

	public float CurrentPrice
	{
		get
		{
			if (!Plan.TryGetUniformPrice(Suggestion.itemName, out var price))
			{
				return -1f;
			}
			return price;
		}
	}

	public float SuggestedPrice => Suggestion.suggestedMax;

	public float RivalsPrice
	{
		get
		{
			if (!(Suggestion.rivalReferencePrice > 0f))
			{
				return -1f;
			}
			return Suggestion.rivalReferencePrice;
		}
	}

	public PricingManagerProductModel(PriceSuggestion suggestion, PricingManagerPlan plan)
	{
		Suggestion = suggestion;
		Plan = plan;
		ProductName = suggestion.itemName.GetLocalization();
	}

	public bool SellsForBusinessType(string businessType)
	{
		return Suggestion.sellingBusinessTypes.Contains(businessType);
	}
}
