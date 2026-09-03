using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using EmployeeStations;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class QueueToolSetup : ToolSetup
{
	private QueueActionPanelUi _queueActionPanel;

	private QueueOverlay _queueOverlay;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Queue;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_queueOverlay = overlay as QueueOverlay;
		_queueActionPanel = actionPanel as QueueActionPanelUi;
		if (_queueOverlay == null || _queueActionPanel == null)
		{
			Debug.LogError("QueueToolSetup: Invalid overlay or action panel type.");
			return;
		}
		Tool = new QueueTool
		{
			itemHasQueue = ItemHasQueue,
			openQueueOverlay = OpenQueueOverlay,
			closeQueueOverlay = CloseQueueOverlay,
			performQueueClick = delegate
			{
				PerformQueueClick(left: true);
			},
			interactQueueClick = delegate
			{
				PerformQueueClick(left: false);
			},
			getEditingItemIndex = GetEditingItemIndex,
			isOverlayOpen = () => _queueOverlay != null && _queueOverlay.isOpen
		};
		QueueRevertibleAction.setQueueAnchors = SetAnchors;
		_queueOverlay.onChangesConfirmed.AddListener(QueueTool.OnQueueChanged);
		_queueOverlay.selectInActionPanel = SelectInActionPanel;
		_queueActionPanel.selectWithItem = SelectWithItem;
	}

	private bool SetAnchors(int itemIndex, List<Vector3> anchors)
	{
		if (_queueOverlay == null)
		{
			return false;
		}
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (!itemControllerAtIndex.Item.hasWaitingLine || !itemControllerAtIndex.TryGetComponent<IWaitingLineHolder>(out var component))
		{
			return false;
		}
		component.GetWaitingLine().SetCustomAnchors(anchors, _queueOverlay.IsOpenWithIndex(itemIndex));
		_queueOverlay.OnSetAnchors();
		return true;
	}

	private bool ItemHasQueue(int itemIndex)
	{
		return GetItemControllerAtIndex(itemIndex).Item.hasWaitingLine;
	}

	private void OpenQueueOverlay(int itemIndex)
	{
		if (!(_queueOverlay == null))
		{
			_queueOverlay.Open(GetItemControllerAtIndex(itemIndex), itemIndex);
		}
	}

	private void CloseQueueOverlay()
	{
		if (!(_queueOverlay == null))
		{
			_queueOverlay.Close();
		}
	}

	private void PerformQueueClick(bool left)
	{
		if (!(_queueOverlay == null) && _queueOverlay.ItemIndex >= 0)
		{
			WaitingLine waitingLine = GetItemControllerAtIndex(_queueOverlay.ItemIndex).GetComponent<IWaitingLineHolder>().GetWaitingLine();
			if (left)
			{
				waitingLine.LeftClick();
			}
			else
			{
				waitingLine.RightClick();
			}
		}
	}

	private int GetEditingItemIndex()
	{
		if (_queueOverlay == null || !_queueOverlay.isOpen)
		{
			return -1;
		}
		return _queueOverlay.ItemIndex;
	}

	private void SelectInActionPanel(ItemController itemController)
	{
		if (!(_queueActionPanel == null))
		{
			_queueActionPanel.SelectFocusButton(itemController);
		}
	}

	private void SelectWithItem(ItemController itemController)
	{
		if (!(_queueOverlay == null))
		{
			_queueOverlay.Open(itemController, GetIndexOfItemController(itemController));
		}
	}
}
