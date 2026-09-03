using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner;
using UnityEngine;

namespace UI.InteriorDesigner;

public class InteriorDesignerActionPanelUI : MonoBehaviour
{
	[SerializeField]
	private List<ActionPanelUI> actionPanels;

	private ActionPanelUI _currentPanel;

	public void Show(ToolName tool)
	{
		if (_currentPanel != null)
		{
			_currentPanel.OnClose();
		}
		bool flag = false;
		if (tool != ToolName.None)
		{
			foreach (ActionPanelUI actionPanel in actionPanels)
			{
				if (actionPanel.ToolNames.Contains(tool))
				{
					actionPanel.OnOpen();
					base.gameObject.SetActive(value: true);
					_currentPanel = actionPanel;
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			_currentPanel = null;
			base.gameObject.SetActive(value: false);
		}
	}

	public void OnOpenInteriorDesigner()
	{
		foreach (ActionPanelUI actionPanel in actionPanels)
		{
			actionPanel.gameObject.SetActive(value: false);
			actionPanel.OnEnterInteriorDesignerMode();
		}
	}
}
