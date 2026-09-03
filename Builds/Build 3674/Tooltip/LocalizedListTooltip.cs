using System.Collections.Generic;

namespace Tooltip;

public class LocalizedListTooltip : BasicTooltip
{
	public List<string> list;

	protected override void ShowTooltip()
	{
		base.ShowTooltip();
		List<string> list = this.list;
		if (list != null && list.Count > 0)
		{
			TooltipSystem.AddList(this.list);
		}
	}
}
