using Localizor;
using UnityEngine;

namespace Extensions;

public static class ColoredTextHelper
{
	public static string GetDemandText(int fulfilled, int total)
	{
		Color32 color = ((fulfilled == 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((fulfilled != total) ? InstanceBehavior<GlobalReferences>.Instance.colors.yellow : InstanceBehavior<GlobalReferences>.Instance.colors.green));
		Color32 color2 = color;
		return string.Format("{0}:", "itemoverlay_employee_details_demand".Localize()) + $" <color=#{ColorUtility.ToHtmlStringRGB(color2)}>{fulfilled}/{total}</color>";
	}

	public static string GetSatisfactionText(float satisfaction)
	{
		Color32 color = ((satisfaction < 30f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(satisfaction < 60f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.yellow));
		Color32 color2 = color;
		return string.Format("{0}:", "myemployees_satisfaction".Localize()) + " <color=#" + ColorUtility.ToHtmlStringRGB(color2) + ">" + $"{Mathf.FloorToInt(satisfaction)}%</color>";
	}

	public static string GetAmountText(int currentAmount, int maxAmount)
	{
		float num = (float)currentAmount / (float)maxAmount * 100f;
		Color32 color = ((num < 25f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(num < 50f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.yellow));
		string arg = ColorUtility.ToHtmlStringRGB(color);
		string arg2 = ((maxAmount >= currentAmount) ? $"{currentAmount}/{maxAmount}" : "-");
		return string.Format("{0}: <color=#{1}>{2}</color>", "common_amount".Localize(), arg, arg2);
	}

	public static string GetAmountTextNumberOnly(int currentAmount, int maxAmount)
	{
		float num = (float)currentAmount / (float)maxAmount * 100f;
		Color32 color = ((num < 25f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(num < 50f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.yellow));
		string text = ColorUtility.ToHtmlStringRGB(color);
		string text2 = ((maxAmount >= currentAmount) ? $"{currentAmount}/{maxAmount}" : "-");
		return "<color=#" + text + ">" + text2 + "</color>";
	}

	public static Color GetBalanceColor(double balance)
	{
		return GetBalanceColor((float)balance);
	}

	public static Color GetBalanceColor(float balance)
	{
		Color32 color = ((balance < 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(balance > 0f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.green));
		return color;
	}

	public static Color GetInteriorScoreColor(float interiorScore)
	{
		Color32 color = ((interiorScore < 25f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(interiorScore < 60f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.yellow));
		return color;
	}
}
