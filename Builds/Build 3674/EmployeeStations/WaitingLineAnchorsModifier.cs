using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

namespace EmployeeStations;

public class WaitingLineAnchorsModifier
{
	private readonly WaitingLineData _data;

	public WaitingLineAnchorsModifier(WaitingLineData data)
	{
		_data = data;
	}

	public void OnLeftClickPressed()
	{
		if (_data.isModifyingSelectedAnchor)
		{
			StopModifying();
		}
		else
		{
			StartModifying();
		}
	}

	public void UpdateAnchorPosition()
	{
		if (Physics.Raycast(GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition), out var hitInfo, 600f, LayerHelper.groundLayerMask) && _data.anchorsPositions[_data.anchorSelected.Index] != hitInfo.point)
		{
			_data.anchorsPositions[_data.anchorSelected.Index] = hitInfo.point;
			_data.onSelectedAnchorPositionUpdated();
			_data.anchorSelected.transform.position = _data.GetAnchorPosition(_data.anchorSelected.Index);
		}
	}

	public void Cancel()
	{
		if (!(_data.anchorSelected == null) && _data.isModifyingSelectedAnchor)
		{
			if (_data.initialPositionOfAnchorModifying == new Vector3(-1f, -1f, -1f))
			{
				_data.anchorsPositions.RemoveAt(_data.anchorSelected.Index);
			}
			else
			{
				_data.anchorsPositions[_data.anchorSelected.Index] = _data.initialPositionOfAnchorModifying;
			}
			StopModifying();
		}
	}

	public void StartModifying()
	{
		if (!(_data.anchorSelected == null))
		{
			_data.isModifyingSelectedAnchor = true;
			_data.isAcceptingNewAnchors = false;
			_data.initialPositionOfAnchorModifying = _data.anchorsPositions[_data.anchorSelected.Index];
			_data.anchorSelected.SetSelectedColor();
		}
	}

	public void StopModifying()
	{
		if (!(_data.anchorSelected == null))
		{
			_data.isModifyingSelectedAnchor = false;
			_data.anchorSelected.SetDefaultColor();
			_data.anchorSelected = null;
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				_data.isAcceptingNewAnchors = true;
			});
			Physics.SyncTransforms();
		}
	}

	public void RemoveSelectedAnchor()
	{
		if (!(_data.anchorSelected == null) && RemoveAnchor(_data.anchorSelected.Index))
		{
			_data.anchorSelected = null;
			_data.isModifyingSelectedAnchor = false;
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				_data.isAcceptingNewAnchors = true;
			});
			Physics.SyncTransforms();
		}
	}

	public bool RemoveAnchor(int positionToRemove)
	{
		if (positionToRemove == 0 || _data.anchorsPositions.Count <= 2)
		{
			return false;
		}
		_data.anchorsPositions.RemoveAt(positionToRemove);
		_data.onAnchorRemoved();
		return true;
	}
}
