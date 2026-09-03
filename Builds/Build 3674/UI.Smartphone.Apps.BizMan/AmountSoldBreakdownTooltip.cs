using System.Collections.Generic;
using Tooltip;

namespace UI.Smartphone.Apps.BizMan;

public class AmountSoldBreakdownTooltip : BasicTooltip
{
	public List<(string, object)> breakdown;

	protected override void ShowTooltip()
	{
		base.ShowTooltip();
		List<(string, object)> list = breakdown;
		if (list != null && list.Count > 0)
		{
			TooltipSystem.AddList(breakdown);
		}
	}
}
