using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerFilterToggle : BaseFilterToggle<PricingManagerProductModel>
{
	private string _businessType;

	public void ConfigureBusinessType(string businessType)
	{
		_businessType = businessType;
		label.Key = businessType;
	}

	public override bool PassesFilter(PricingManagerProductModel model)
	{
		return model.SellsForBusinessType(_businessType);
	}
}
