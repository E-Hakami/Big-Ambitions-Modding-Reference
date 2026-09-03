using System.Collections.Generic;
using UnityEngine;

namespace UI.InteriorDesigner;

public class InteriorDesignerInfoPanelUI : MonoBehaviour
{
	[SerializeField]
	private List<InfoPanelUI> infoPanels;

	public void OnOpenInteriorDesigner()
	{
		foreach (InfoPanelUI infoPanel in infoPanels)
		{
			bool flag = infoPanel.ShouldShow();
			infoPanel.gameObject.SetActive(flag);
			if (flag)
			{
				infoPanel.OnEnterInteriorDesignerMode();
			}
		}
	}

	public void OnExitInteriorDesigner()
	{
		foreach (InfoPanelUI infoPanel in infoPanels)
		{
			infoPanel.gameObject.SetActive(value: false);
			infoPanel.OnExitInteriorDesignerMode();
		}
	}
}
