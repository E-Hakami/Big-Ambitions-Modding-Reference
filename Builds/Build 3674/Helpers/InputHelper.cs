using System;
using BigAmbitions.InputSystem;
using NWH.Common.Input;
using NWH.VehiclePhysics2.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Helpers;

public static class InputHelper
{
	private const string VehicleTag = "Vehicle";

	public static PlayerInput playerInput;

	public static bool autoRunToggled;

	public static bool IsInitialized()
	{
		return playerInput != null;
	}

	public static void SetupPlayerInput()
	{
		playerInput?.Disable();
		playerInput?.Dispose();
		playerInput = new PlayerInput();
		playerInput.Enable();
		InputActionHelper.PlayerInputActionMap.Clear();
		InputActionHelper.InteriorDesignerInputActionMap.Clear();
		foreach (PlayerAction value in Enum.GetValues(typeof(PlayerAction)))
		{
			InputActionHelper.PlayerInputActionMap.Add(value, playerInput.FindAction(value.ToStringFast()));
		}
		foreach (InteriorDesignerAction value2 in Enum.GetValues(typeof(InteriorDesignerAction)))
		{
			InputActionHelper.InteriorDesignerInputActionMap.Add(value2, playerInput.FindAction(value2.ToStringFast()));
		}
		UpdateBindings();
		foreach (InputProvider instance in InputProvider.Instances)
		{
			if ((bool)instance && instance is InputSystemVehicleInputProvider inputSystemVehicleInputProvider)
			{
				inputSystemVehicleInputProvider.mouseInput = PlayerPrefSettings.VehicleMouseInput;
			}
		}
	}

	public static void UpdateBindings()
	{
		if (playerInput != null && playerInput.asset != null)
		{
			RebindSaveLoad.LoadBindingOverrides(playerInput.asset);
		}
		if (InputSystemVehicleInputProvider.vehicleInputActions != null && InputSystemVehicleInputProvider.vehicleInputActions.asset != null)
		{
			RebindSaveLoad.LoadBindingOverrides(InputSystemVehicleInputProvider.vehicleInputActions.asset);
		}
		GlobalEvents.onBindingsChanged?.Invoke();
	}

	public static T GetClickedComponent<T>(LayerMask layerMask, bool includeParent)
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return default(T);
		}
		if (!Physics.Raycast(GameManager.GetMainCamera().ScreenPointToRay(InputActionHelper.GetCursorPosition()), out var hitInfo, 600f, layerMask))
		{
			return default(T);
		}
		T component = hitInfo.collider.gameObject.GetComponent<T>();
		if (component != null || !includeParent)
		{
			return component;
		}
		T componentInParent = hitInfo.collider.transform.GetComponentInParent<T>();
		if (componentInParent == null)
		{
			return default(T);
		}
		return componentInParent;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		playerInput = null;
		autoRunToggled = false;
	}
}
