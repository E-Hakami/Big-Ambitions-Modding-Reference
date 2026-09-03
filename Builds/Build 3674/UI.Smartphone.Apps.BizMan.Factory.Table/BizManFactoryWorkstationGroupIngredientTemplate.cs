using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory.Table;

public class BizManFactoryWorkstationGroupIngredientTemplate : MonoBehaviour
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private TMP_Text itemNameText;

	[SerializeField]
	private TMP_Text inStockText;

	[SerializeField]
	private TMP_Text runsOutInText;

	public void SetUp(BizManFactoryWorkstationGroupModelIngredient data)
	{
		itemImage.sprite = ItemHelper.GetIconWithFallback(data.itemName);
		itemNameText.text = data.itemName.GetLocalization();
		TMP_Text tMP_Text = inStockText;
		int inStock = data.inStock;
		tMP_Text.text = inStock.ToString();
		inStockText.color = ((data.inStock > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		if (data.runsOutInDays == -1)
		{
			runsOutInText.SetText("bizman_inventory_run_out".GetLocalization());
			runsOutInText.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
		else if (data.runsOutInDays == int.MaxValue)
		{
			runsOutInText.SetText("bizman_inventory_never_runs_out".GetLocalization());
			runsOutInText.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		}
		else
		{
			runsOutInText.SetText((data.runsOutInDays == 0) ? "common_today".GetLocalization() : "bizman_inventory_product_days_until_empty".Localize(new
			{
				days = data.runsOutInDays
			}).ToString());
			runsOutInText.color = ((data.runsOutInDays > 1) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.yellow);
		}
	}
}
