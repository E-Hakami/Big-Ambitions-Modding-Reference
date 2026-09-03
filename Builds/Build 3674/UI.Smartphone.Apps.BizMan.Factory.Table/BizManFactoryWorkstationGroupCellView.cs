using System;
using System.Collections.Generic;
using BaTable;
using Extensions;
using Localizor;
using TMPro;
using UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory.Table;

public class BizManFactoryWorkstationGroupCellView : BaTableCellView<BizManFactoryWorkstationGroupModel>
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private TMP_Text itemNameText;

	[SerializeField]
	private TMP_Text producedPerHourText;

	[SerializeField]
	private TMP_Text inStockText;

	[SerializeField]
	private TMP_Text runsOutInText;

	[SerializeField]
	private BizManFactoryWorkstationGroupIngredientTemplate ingredientTemplate;

	[SerializeField]
	private CustomGameFoldout foldout;

	[SerializeField]
	private Color foldedYellow;

	private BizManFactoryWorkstationGroupModel _data;

	private void Start()
	{
		CustomGameFoldout customGameFoldout = foldout;
		customGameFoldout.onToggleFoldout = (Action<bool>)Delegate.Combine(customGameFoldout.onToggleFoldout, new Action<bool>(OnToggleFoldout));
	}

	private void OnDestroy()
	{
		CustomGameFoldout customGameFoldout = foldout;
		customGameFoldout.onToggleFoldout = (Action<bool>)Delegate.Remove(customGameFoldout.onToggleFoldout, new Action<bool>(OnToggleFoldout));
	}

	public override void SetData(BizManFactoryWorkstationGroupModel data)
	{
		_data = data;
		itemImage.sprite = ItemHelper.GetIconWithFallback(data.itemName);
		itemNameText.SetText(data.itemName.GetLocalization());
		TMP_Text tMP_Text = producedPerHourText;
		int producedPerHour = data.producedPerHour;
		tMP_Text.text = producedPerHour.ToString();
		producedPerHourText.color = ((data.producedPerHour > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		TMP_Text tMP_Text2 = inStockText;
		producedPerHour = data.inStock;
		tMP_Text2.text = producedPerHour.ToString();
		inStockText.color = ((data.inStock > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		if (data.runsOutInDays == -1)
		{
			runsOutInText.SetText("bizman_inventory_run_out".GetLocalization());
		}
		else if (data.runsOutInDays == int.MaxValue)
		{
			runsOutInText.SetText("bizman_inventory_never_runs_out".GetLocalization());
		}
		else
		{
			runsOutInText.SetText((data.runsOutInDays == 0) ? "common_today".GetLocalization() : "bizman_inventory_product_days_until_empty".Localize(new
			{
				days = data.runsOutInDays
			}).ToString());
		}
		ingredientTemplate.transform.ResetTemplate();
		foreach (BizManFactoryWorkstationGroupModelIngredient ingredient in _data.ingredients)
		{
			BizManFactoryWorkstationGroupIngredientTemplate bizManFactoryWorkstationGroupIngredientTemplate = UnityEngine.Object.Instantiate(ingredientTemplate, ingredientTemplate.transform.parent);
			bizManFactoryWorkstationGroupIngredientTemplate.SetUp(ingredient);
			bizManFactoryWorkstationGroupIngredientTemplate.gameObject.SetActive(value: true);
		}
		foldout.SetExpanded(_data.scroller.foldoutStates.GetValueOrDefault(_data.index, defaultValue: false));
		_data.scroller.foldoutStates[_data.index] = foldout.IsExpanded;
		UpdateColors();
	}

	private void OnToggleFoldout(bool isExpanded)
	{
		_data.scroller.foldoutStates[_data.index] = isExpanded;
		_data.scroller.scroller.ReloadData(_data.scroller.scroller.NormalizedScrollPosition);
	}

	private void UpdateColors()
	{
		producedPerHourText.color = ((_data.producedPerHour > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		inStockText.color = ((_data.inStock > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		if (_data.runsOutInDays == -1)
		{
			runsOutInText.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
		else if (_data.runsOutInDays == int.MaxValue)
		{
			runsOutInText.color = InstanceBehavior<GlobalReferences>.Instance.colors.black;
		}
		else
		{
			runsOutInText.color = ((_data.runsOutInDays > 1) ? ((Color)InstanceBehavior<GlobalReferences>.Instance.colors.black) : foldedYellow);
		}
	}
}
