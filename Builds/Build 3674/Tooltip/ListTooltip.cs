using System.Collections.Generic;

namespace Tooltip;

public class ListTooltip : BasicTooltip
{
	public List<string> list;

	protected override void ShowTooltip()
	{
		base.ShowTooltip();
		List<string> list = this.list;
		if (list != null && list.Count > 0)
		{
			TooltipSystem.AddSplitter();
			TooltipSystem.AddList(this.list);
		}
	}
}
