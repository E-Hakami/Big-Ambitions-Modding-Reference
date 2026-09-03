using System;
using System.Collections.Generic;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewIncomeStatementModel
{
	public readonly bool autoSetValue1Color;

	public readonly float height;

	public readonly Action onClick;

	public readonly EconoViewIncomeStatementModel parent;

	public readonly string rowName;

	public readonly EconoViewRowType rowType;

	public readonly List<float> values;

	public bool isExpanded = true;

	public string name;

	public EconoViewIncomeStatementModel(EconoViewRowType rowType, string rowName, List<float> values, bool autoSetValue1Color, EconoViewIncomeStatementModel parent, Action onClick, float height)
	{
		this.rowType = rowType;
		this.rowName = rowName;
		this.values = CopyValues(values);
		this.autoSetValue1Color = autoSetValue1Color;
		this.parent = parent;
		this.onClick = onClick;
		this.height = height;
		name = rowName;
	}

	public void InvokeClick()
	{
		onClick?.Invoke();
	}

	private static List<float> CopyValues(List<float> values)
	{
		List<float> list = new List<float>(4) { 0f, 0f, 0f, 0f };
		if (values == null)
		{
			return list;
		}
		int num = ((values.Count < list.Count) ? values.Count : list.Count);
		for (int i = 0; i < num; i++)
		{
			list[i] = values[i];
		}
		return list;
	}
}
