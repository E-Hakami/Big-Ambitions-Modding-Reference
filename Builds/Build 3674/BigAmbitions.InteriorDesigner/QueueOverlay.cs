using System;
using System.Collections.Generic;
using EmployeeStations;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigAmbitions.InteriorDesigner;

public class QueueOverlay : MonoBehaviour
{
	[SerializeField]
	private UIHoverAboveObject hoverAboveObject;

	[SerializeField]
	private Button resetQueueButton;

	[SerializeField]
	private TMP_Text titleText;

	public readonly UnityEvent<int, List<Vector3>, List<Vector3>> onChangesConfirmed = new UnityEvent<int, List<Vector3>, List<Vector3>>();

	public Action<ItemController> selectInActionPanel;

	private List<Vector3> _initialAnchorsPositions = new List<Vector3>();

	private WaitingLine _waitingLine;

	[HideInInspector]
	public bool isOpen;

	public int ItemIndex { get; private set; } = -1;

	public bool IsOpenWithIndex(int index)
	{
		if (isOpen)
		{
			return ItemIndex == index;
		}
		return false;
	}

	private bool HasChanges()
	{
		List<Vector3> anchorsPositions = _waitingLine.data.anchorsPositions;
		if (anchorsPositions.Count != _initialAnchorsPositions.Count)
		{
			return true;
		}
		for (int i = 0; i < anchorsPositions.Count; i++)
		{
			if (!Mathf.Approximately(anchorsPositions[i].x, _initialAnchorsPositions[i].x) || !Mathf.Approximately(anchorsPositions[i].z, _initialAnchorsPositions[i].z))
			{
				return true;
			}
		}
		return false;
	}

	public void Open(ItemController itemController, int itemIndex)
	{
		if (itemController == null)
		{
			return;
		}
		if (isOpen)
		{
			if (HasChanges())
			{
				onChangesConfirmed.Invoke(ItemIndex, _initialAnchorsPositions, _waitingLine.data.anchorsPositions);
			}
			_waitingLine.visuals.Hide();
		}
		ItemIndex = itemIndex;
		_waitingLine = itemController.GetComponent<IWaitingLineHolder>().GetWaitingLine();
		hoverAboveObject.SetObjectToFollow(itemController.transform);
		_initialAnchorsPositions = _waitingLine.data.anchorsPositions.Copy();
		titleText.text = itemController.itemName.Localize().ToString();
		_waitingLine.creator.SetUpWaitingLine();
		_waitingLine.visuals.Show();
		selectInActionPanel?.Invoke(itemController);
		base.gameObject.SetActive(value: true);
		isOpen = true;
	}

	public void Close()
	{
		if (isOpen)
		{
			isOpen = false;
			_waitingLine.Close();
			if (HasChanges())
			{
				onChangesConfirmed.Invoke(ItemIndex, _initialAnchorsPositions, _waitingLine.data.anchorsPositions);
				_initialAnchorsPositions = _waitingLine.data.anchorsPositions.Copy();
			}
			ItemIndex = -1;
			base.gameObject.SetActive(value: false);
		}
	}

	public void OnSetAnchors()
	{
		_initialAnchorsPositions = _waitingLine.data.anchorsPositions.Copy();
	}

	public void ResetQueue()
	{
		_initialAnchorsPositions = _waitingLine.data.anchorsPositions.Copy();
		_waitingLine.ResetWaitingLine();
		if (HasChanges())
		{
			onChangesConfirmed.Invoke(ItemIndex, _initialAnchorsPositions, _waitingLine.data.anchorsPositions);
			_initialAnchorsPositions = _waitingLine.data.anchorsPositions.Copy();
		}
	}
}
