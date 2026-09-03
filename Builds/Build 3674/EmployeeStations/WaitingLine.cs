using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

namespace EmployeeStations;

public class WaitingLine : MonoBehaviour
{
	public WaitingLineData data;

	public WaitingLineTransforms transforms;

	public WaitingLineCreator creator;

	public WaitingLineVisuals visuals;

	public WaitingLineCustomersManagement customersManagement;

	public WaitingLineAnchorsModifier anchorsModifier;

	private bool _initialized;

	private Camera _cam;

	public ItemController ItemController { get; private set; }

	public IWaitingLineHolder WaitingLineHolder => ItemController as IWaitingLineHolder;

	public EmployeeStationController EmployeeStationController => ItemController as EmployeeStationController;

	public void Init(ItemController itemController, List<SerializableVector3> anchorPositions, string[] controllerNames, Action onDataChanged)
	{
		if (!itemController)
		{
			Debug.LogError("WaitingLine Init failed: ItemController is null", this);
			return;
		}
		_cam = GameManager.GetMainCamera();
		ItemController = itemController;
		data = new WaitingLineData(anchorPositions, controllerNames, onDataChanged);
		creator = new WaitingLineCreator(WaitingLineHolder);
		visuals = new WaitingLineVisuals(data, transforms);
		customersManagement = new WaitingLineCustomersManagement(data, transforms);
		anchorsModifier = new WaitingLineAnchorsModifier(data);
		_initialized = true;
	}

	public bool MatchesControllerName(string controllerName)
	{
		return data.controllerNames.Contains(controllerName);
	}

	public void ResetWaitingLine()
	{
		anchorsModifier.Cancel();
		creator.Reset();
		visuals.Show();
	}

	public void SetCustomAnchors(List<Vector3> customPositions, bool showChanges)
	{
		data.SetCustomAnchors(customPositions);
		if (showChanges)
		{
			creator.SetUpWaitingLine();
			visuals.Show();
		}
	}

	public void AddNewAnchor()
	{
		anchorsModifier.StopModifying();
		data.initialPositionOfAnchorModifying = new SerializableVector3(-1f, -1f, -1f);
		data.anchorsPositions.Add(data.initialPositionOfAnchorModifying);
		data.anchorSelected = visuals.AddAnchor(data.anchorsPositions.Count - 1);
		anchorsModifier.StartModifying();
	}

	private void AddNewAnchorAtPosition(Vector3 position)
	{
		anchorsModifier.StopModifying();
		data.initialPositionOfAnchorModifying = position;
		int previousAnchorIndex = GetPreviousAnchorIndex(position);
		previousAnchorIndex++;
		data.anchorsPositions.Insert(previousAnchorIndex, data.initialPositionOfAnchorModifying);
		visuals.ResetAnchors();
		data.anchorSelected = visuals.GetAnchorAtIndex(previousAnchorIndex);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			anchorsModifier.StartModifying();
		});
	}

	private int GetPreviousAnchorIndex(Vector3 position)
	{
		int result = 0;
		Vector3 positionXZ = new Vector3(position.x, 0f, position.z);
		int num = data.spots.FindIndex((Vector3 spot) => new Vector3(spot.x, 0f, spot.z) == positionXZ);
		for (int num2 = 0; num2 < num; num2++)
		{
			Vector3 spotXZ = new Vector3(data.spots[num2].x, 0f, data.spots[num2].z);
			int num3 = data.anchorsPositions.FindIndex((Vector3 anchor) => new Vector3(anchor.x, 0f, anchor.z) == spotXZ);
			if (num3 != -1)
			{
				result = num3;
			}
		}
		return result;
	}

	public void Close()
	{
		anchorsModifier.Cancel();
		visuals.Hide();
	}

	private void Update()
	{
		if (_initialized)
		{
			if (data.isModifyingSelectedAnchor)
			{
				anchorsModifier.UpdateAnchorPosition();
			}
			else
			{
				UpdateHoveredAnchor();
			}
		}
	}

	private void UpdateHoveredAnchor()
	{
		if (!Physics.Raycast(GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition), out var hitInfo, 600f))
		{
			if (!(data.anchorSelected == null) && !data.isModifyingSelectedAnchor)
			{
				data.anchorSelected.OnIoExit();
				data.anchorSelected = null;
			}
			return;
		}
		WaitingLineAnchor component = hitInfo.transform.GetComponent<WaitingLineAnchor>();
		if (component == data.anchorSelected)
		{
			return;
		}
		if (component == null)
		{
			if (!(data.anchorSelected == null) && !data.isModifyingSelectedAnchor)
			{
				data.anchorSelected.OnIoExit();
				data.anchorSelected = null;
			}
			return;
		}
		if (data.anchorSelected != null)
		{
			data.anchorSelected.OnIoExit();
		}
		data.anchorSelected = component;
		data.anchorSelected.OnIoEnter();
	}

	public void LeftClick()
	{
		anchorsModifier.OnLeftClickPressed();
		if (data.anchorSelected != null || !data.isAcceptingNewAnchors)
		{
			return;
		}
		if (!IsMouseCloseToLine())
		{
			AddNewAnchor();
			return;
		}
		Vector3 spotClosestToMouse = GetSpotClosestToMouse();
		Vector3 vector = -Vector3.one;
		foreach (Vector3 anchorsPosition in data.anchorsPositions)
		{
			if (Mathf.Approximately(anchorsPosition.x, spotClosestToMouse.x) && Mathf.Approximately(anchorsPosition.z, spotClosestToMouse.z))
			{
				vector = anchorsPosition;
				break;
			}
		}
		if (vector != -Vector3.one)
		{
			int relativeAnchorIndex = GetRelativeAnchorIndex(vector);
			if (relativeAnchorIndex > 0)
			{
				visuals.EditAnchor(relativeAnchorIndex);
				anchorsModifier.StartModifying();
				return;
			}
		}
		AddNewAnchorAtPosition(spotClosestToMouse);
	}

	public void RightClick()
	{
		if (data.anchorSelected != null)
		{
			anchorsModifier.RemoveSelectedAnchor();
		}
		else if (IsMouseCloseToLine())
		{
			Vector3 spot = GetSpotClosestToMouse();
			Vector3 vector = data.anchorsPositions.FirstOrDefault((Vector3 x) => Mathf.Approximately(x.x, spot.x) && Mathf.Approximately(x.z, spot.z));
			if (vector != Vector3.zero)
			{
				anchorsModifier.RemoveAnchor(GetRelativeAnchorIndex(vector));
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (data?.spots != null)
		{
			Color red = Color.red;
			Color green = Color.green;
			for (int i = 0; i < data.spots.Count - 1; i++)
			{
				float t = (float)i / (float)(data.spots.Count - 1);
				Gizmos.color = Color.Lerp(red, green, t);
				Gizmos.DrawSphere(data.spots[i], 0.2f);
				Gizmos.DrawLine(data.spots[i], data.spots[i + 1]);
			}
			if (data.spots.Count > 0)
			{
				Gizmos.color = green;
				List<Vector3> spots = data.spots;
				Gizmos.DrawSphere(spots[spots.Count - 1], 0.2f);
			}
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(GetSpotClosestToMouse(), 0.2f);
		}
	}

	private Vector3 GetSpotClosestToMouse()
	{
		Vector3 mouseWorldPosition = GetMouseWorldPosition();
		Vector3 vector = data.spots[0];
		float num = Vector3.SqrMagnitude(mouseWorldPosition - vector);
		for (int i = 1; i < data.spots.Count; i++)
		{
			float num2 = Vector3.SqrMagnitude(mouseWorldPosition - data.spots[i]);
			if (num2 < num)
			{
				vector = data.spots[i];
				num = num2;
			}
		}
		return vector;
	}

	public bool IsMouseCloseToLine()
	{
		Vector3 mouseWorldPosition = GetMouseWorldPosition();
		float num = 0.3f;
		for (int i = 0; i < data.spots.Count - 1; i++)
		{
			if (DistanceFromPointToLineSegment(mouseWorldPosition, data.spots[i], data.spots[i + 1]) <= num)
			{
				return true;
			}
		}
		return false;
	}

	private Vector3 GetMouseWorldPosition()
	{
		if (!Physics.Raycast(_cam.ScreenPointToRay(InputActionHelper.GetCursorPosition()), out var hitInfo, 500f, LayerHelper.groundLayerMask))
		{
			return Vector3.zero;
		}
		return hitInfo.point;
	}

	private float DistanceFromPointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
	{
		Vector3 vector = lineEnd - lineStart;
		Vector3 lhs = point - lineStart;
		float magnitude = vector.magnitude;
		if (magnitude != 0f)
		{
			vector /= magnitude;
		}
		float value = Vector3.Dot(lhs, vector);
		value = Mathf.Clamp(value, 0f, magnitude);
		Vector3 b = lineStart + value * vector;
		return Vector3.Distance(point, b);
	}

	private int GetRelativeAnchorIndex(Vector3 position)
	{
		Vector3 positionXZ = new Vector3(position.x, 0f, position.z);
		for (int num = data.spots.FindIndex((Vector3 spot) => new Vector3(spot.x, 0f, spot.z) == positionXZ); num < data.spots.Count; num++)
		{
			Vector3 spotXZ = new Vector3(data.spots[num].x, 0f, data.spots[num].z);
			int num2 = data.anchorsPositions.FindIndex((Vector3 anchor) => new Vector3(anchor.x, 0f, anchor.z) == spotXZ);
			if (num2 != -1)
			{
				return num2;
			}
		}
		return -1;
	}

	public void RedirectCustomersToGivenWaitingLine(WaitingLine newWaitingLine, ref int spotsAvailable)
	{
		Customer[] array = data.customers.OrderByDescending((Customer x) => x.currentWaitingLineSpot).ToArray();
		foreach (Customer customer in array)
		{
			if (newWaitingLine.data.customers.Count + 1 < data.customers.Count && spotsAvailable != 0)
			{
				customersManagement.MoveCustomerToGivenEmployeeStation(customer, newWaitingLine);
				spotsAvailable--;
				continue;
			}
			break;
		}
	}
}
