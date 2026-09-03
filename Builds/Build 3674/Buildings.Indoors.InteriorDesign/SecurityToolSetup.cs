using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Tags;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class SecurityToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Security;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new SecurityTool
		{
			getSecurityItems = GetSecurityItems,
			getSelectedCameraIndex = GetSelectedCameraIndex,
			startMovingCamera = delegate(int i)
			{
				IInteriorDesignerTool.moveItemWithHandTool(i, null, null);
			}
		};
	}

	private static List<GameObject> GetSecurityItems()
	{
		if (InteriorDesignerController.SecurityItemsCache.Count <= 0)
		{
			return null;
		}
		return InteriorDesignerController.SecurityItemsCache.Select((ItemController x) => x.gameObject).ToList();
	}

	private int GetSelectedCameraIndex()
	{
		int num = IInteriorDesignerTool.getSelectedItemControllerIndex();
		if (num == -1)
		{
			return -1;
		}
		if (!GetItemControllerAtIndex(num).Item.HasTag(TagRef.Itemtag.issecuritycamera))
		{
			return -1;
		}
		return num;
	}
}
