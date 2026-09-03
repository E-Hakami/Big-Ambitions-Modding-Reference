using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.PlacementSystem;
using Helpers;
using NaughtyAttributes;
using UI;
using UI.InteriorDesigner;
using UI.Load;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class MouseController
{
	private const float DistanceSqrInPixelsToConsiderButtonIsBeingHeld = 49f;

	[ReadOnly]
	public static CursorType cursorOnHover = CursorType.Default;

	private static readonly Dictionary<CursorType, CursorData> CursorDataDict = new Dictionary<CursorType, CursorData>();

	private static CursorType CurrentCursorType = CursorType.Default;

	private static Transform CurrentTarget;

	private static ICursorHoverEvent CursorHoverChangeEvent;

	private static CursorType DefaultCursor = CursorType.Default;

	private static InteractiveObjectType[] DefaultObjectTypes;

	private static float LastMouseMoveTime;

	private static Vector3 LastMousePosition;

	private static bool LeftButtonHeld;

	private static Vector2 MousePositionWhenStartedClick;

	private static LayerMask RaycastLayerMark;

	private static int RaycastLayerMask;

	private static bool RightButtonHeld;

	private static MouseSettings MouseSettings;

	private static bool Initialized;

	[NonSerialized]
	public static EntityController currentTargetEntity;

	public static void Reset()
	{
		cursorOnHover = CursorType.Default;
		SetRestrictedObjectTypes(DefaultObjectTypes);
	}

	public static void Init(MouseSettings mouseSettings)
	{
		if (Initialized)
		{
			return;
		}
		MouseSettings = mouseSettings;
		CursorDataDict.Clear();
		foreach (CursorData cursor in MouseSettings.cursors)
		{
			CursorDataDict.Add(cursor.type, cursor);
		}
		DefaultObjectTypes = new InteractiveObjectType[7]
		{
			InteractiveObjectType.InteractiveItems,
			InteractiveObjectType.InteractiveItemsOutlined,
			InteractiveObjectType.Buildings,
			InteractiveObjectType.BuildingsOutlined,
			InteractiveObjectType.Vehicles,
			InteractiveObjectType.PlayerVehicles,
			InteractiveObjectType.Ground
		};
		Reset();
		Initialized = true;
	}

	public static void Run()
	{
		if (LoadScene.isLoading || Mouse.current == null || PlayerHelper.playerDead)
		{
			return;
		}
		if (PurchaseVehicleUI.IsShowcaseAnimationRunning)
		{
			if (currentTargetEntity != null)
			{
				ResetCurrentEntitySelected(currentTargetEntity);
			}
			SetDefaultCursor(CursorType.Default);
			SetCursor(null);
			return;
		}
		if (CityMap.IsOpen)
		{
			CheckIfBuildingsHighlighted();
		}
		bool flag = true;
		bool flag2 = true;
		VehicleController vehicleController = InstanceBehavior<GameManager>.Instance?.selectedVehicle;
		if (vehicleController != null && !vehicleController.vehicleType.spawnInPlayerObject)
		{
			if (PlayerPrefSettings.VehicleMouseInput)
			{
				if (vehicleController.CurrentSpeed != 0f && !BuildingManager.IsInsideBuilding)
				{
					flag2 = false;
				}
			}
			else if (Input.mousePosition.x != LastMousePosition.x || Input.mousePosition.y != LastMousePosition.y)
			{
				LastMousePosition = Input.mousePosition;
				LastMouseMoveTime = Time.time;
			}
			else if (Time.time - LastMouseMoveTime >= MouseSettings.vehicleMouseHideTime)
			{
				flag = false;
				flag2 = false;
			}
			if (CityMap.IsOpen)
			{
				flag2 = true;
				flag = true;
			}
			if (!flag2 && currentTargetEntity != null)
			{
				currentTargetEntity.OnIoExit();
				currentTargetEntity = null;
			}
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			if (currentTargetEntity != null)
			{
				currentTargetEntity.OnIoExit();
				currentTargetEntity = null;
			}
			return;
		}
		if (flag != Cursor.visible)
		{
			Cursor.visible = flag;
		}
		if (!Cursor.visible || !flag2 || (InstanceBehavior<GameManager>.Instance != null && (InstanceBehavior<GameManager>.Instance.isTrailerMode || !GameManager.IsInFocus || (InstanceBehavior<UIs>.Instance != null && InstanceBehavior<UIs>.Instance.draggableWindows.isCurrentlyDragging))))
		{
			return;
		}
		Ray ray = GameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition);
		bool leftButtonHeld = LeftButtonHeld;
		bool rightButtonHeld = RightButtonHeld;
		DetermineMouseHold();
		if ((leftButtonHeld && !LeftButtonHeld) || (rightButtonHeld && !RightButtonHeld))
		{
			return;
		}
		bool flag3 = Physics.Raycast(ray, out var hitInfo, 600f, RaycastLayerMark);
		if (currentTargetEntity != null && (!flag3 || CurrentTarget != hitInfo.transform))
		{
			ResetCurrentEntitySelected(currentTargetEntity);
		}
		if (flag3 && !EventSystem.current.IsPointerOverGameObject() && (hitInfo.transform.gameObject.layer != LayerHelper.GroundLayerIndex || BuildingManager.IsInsideBuilding))
		{
			SetDefaultCursor(cursorOnHover);
			if (currentTargetEntity == null && hitInfo.transform.TryGetComponent<EntityController>(out currentTargetEntity))
			{
				CurrentTarget = hitInfo.transform;
				currentTargetEntity.OnIoEnter();
				return;
			}
			if (!InteriorDesignerUI.IsOpen)
			{
				if (currentTargetEntity != null && Input.GetMouseButtonUp(1))
				{
					currentTargetEntity.OnIoRightClick();
					return;
				}
				if (!SubwaySystem.IsRiding && currentTargetEntity != null && Input.GetMouseButtonUp(0) && currentTargetEntity.OnIoLeftClick())
				{
					return;
				}
			}
		}
		else
		{
			SetDefaultCursor(CursorType.Default);
		}
		if (InstanceBehavior<GameManager>.Instance == null || InstanceBehavior<GameManager>.Instance.playerController == null || !InstanceBehavior<GameManager>.Instance.playerController.IsOnNavmesh() || GameManager.HasInputSelected() || !Physics.Raycast(ray, out var hitInfo2, 800f, LayerHelper.mouseGroundMask))
		{
			return;
		}
		if (!Input.GetMouseButtonDown(0) && Input.GetMouseButton(0) && (currentTargetEntity == null || !currentTargetEntity.primaryInteractionEnabled) && !EventSystem.current.IsPointerOverGameObject())
		{
			LeftButtonHeld = true;
		}
		if ((currentTargetEntity == null || !currentTargetEntity.primaryInteractionEnabled || (currentTargetEntity is DirtSpotObject && !PlayerHelper.IsHoldingAMop)) && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(hitInfo2.point, showParticleEffect: true);
		}
		else
		{
			if (!LeftButtonHeld && !InputHelper.autoRunToggled)
			{
				return;
			}
			Vector3 position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
			Vector3 vector = hitInfo2.point - position;
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude > MouseSettings.minPlayerMoveClickDistance * MouseSettings.minPlayerMoveClickDistance)
			{
				Vector3 target = hitInfo2.point;
				if (sqrMagnitude < MouseSettings.minPlayerMoveExtrapolateDistance * MouseSettings.minPlayerMoveExtrapolateDistance)
				{
					target = position + vector.normalized * MouseSettings.minPlayerMoveExtrapolateDistance;
				}
				InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(target, showParticleEffect: false);
			}
		}
	}

	private static void CheckIfBuildingsHighlighted()
	{
		bool flag = Physics.Raycast(GameManager.GetMainCamera().ScreenPointToRay(InputActionHelper.GetCursorPosition()), out var hitInfo, 600f, RaycastLayerMark);
		if (currentTargetEntity != null && (!flag || CurrentTarget != hitInfo.transform))
		{
			ResetCurrentEntitySelected(currentTargetEntity);
		}
		if (flag && !EventSystem.current.IsPointerOverGameObject())
		{
			if (currentTargetEntity == null && hitInfo.transform.TryGetComponent<EntityController>(out currentTargetEntity))
			{
				CurrentTarget = hitInfo.transform;
				currentTargetEntity.OnIoEnter();
			}
			if (!(currentTargetEntity == null) && PlayerAction.Interact.Pressed())
			{
				PlayerAction.Interact.Reset();
				currentTargetEntity.OnIoLeftClick();
			}
		}
	}

	private static void DetermineMouseHold()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
		{
			MousePositionWhenStartedClick = Input.mousePosition;
		}
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			InputHelper.autoRunToggled = false;
			if (Vector2.SqrMagnitude(MousePositionWhenStartedClick - (Vector2)Input.mousePosition) > 49f)
			{
				LeftButtonHeld = true;
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			LeftButtonHeld = false;
		}
		if (Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject() && Vector2.SqrMagnitude(MousePositionWhenStartedClick - (Vector2)Input.mousePosition) > 49f)
		{
			RightButtonHeld = true;
		}
		if (Input.GetMouseButtonUp(1))
		{
			RightButtonHeld = false;
		}
	}

	private static void SetDefaultCursor(CursorType cursorType)
	{
		bool num = DefaultCursor != cursorType && CurrentCursorType == DefaultCursor;
		DefaultCursor = cursorType;
		if (num)
		{
			SetCursor(null);
		}
	}

	public static bool SetCursor(ICursorHoverEvent cursorHoverChangeEvent)
	{
		if (CursorHoverChangeEvent != null)
		{
			if (!CursorHoverChangeEvent.ChangedCursor)
			{
				return false;
			}
			CursorHoverChangeEvent.ChangedCursor = false;
			CursorHoverChangeEvent.OnPointerExit(null);
		}
		CursorHoverChangeEvent = cursorHoverChangeEvent;
		CursorType cursorType = ((cursorHoverChangeEvent != null && cursorHoverChangeEvent.CursorType != CursorType.Auto) ? cursorHoverChangeEvent.CursorType : DefaultCursor);
		if (CurrentCursorType == cursorType)
		{
			return true;
		}
		CursorData cursorData = CursorDataDict[cursorType];
		Cursor.SetCursor(cursorData.cursorTexture, cursorData.hotspot, CursorMode.Auto);
		CurrentCursorType = cursorType;
		return true;
	}

	public static void SetRestrictedObjectTypes(params InteractiveObjectType[] types)
	{
		RaycastLayerMark = LayerMask.GetMask(types.Select((InteractiveObjectType x) => x.ToString()).ToArray());
	}

	public static void ResetCurrentEntitySelected(EntityController entity)
	{
		entity.OnIoExit();
		if (!(currentTargetEntity != entity))
		{
			currentTargetEntity = null;
			CurrentTarget = null;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		CursorDataDict.Clear();
		CurrentCursorType = CursorType.Default;
		CurrentTarget = null;
		CursorHoverChangeEvent = null;
		DefaultCursor = CursorType.Default;
		DefaultObjectTypes = null;
		LastMousePosition = Vector3.zero;
		LeftButtonHeld = false;
		RightButtonHeld = false;
		Initialized = false;
		currentTargetEntity = null;
	}
}
