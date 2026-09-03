using GleyTrafficSystem;
using JimmysUnityUtilities;
using Player.HUD.SmartphoneUI;
using UnityEngine;

public class AiCarRescueCheck : MonoBehaviour
{
	private const float SecondsStationaryToDespawnWhenNotVisible = 20f;

	private const float SecondsStationaryToDespawnWhenVisible = 40f;

	[SerializeField]
	private Renderer vehicleRenderer;

	[SerializeField]
	private VehicleComponent vehicleComponent;

	private float _timeBeingStationary;

	private void OnEnable()
	{
		_timeBeingStationary = 0f;
		InstanceBehavior<AiCarRescueCheckController>.Instance.Register(this);
	}

	private void OnDisable()
	{
		_timeBeingStationary = 0f;
		InstanceBehavior<AiCarRescueCheckController>.Instance?.UnRegister(this);
	}

	private void OnDestroy()
	{
		_timeBeingStationary = 0f;
		InstanceBehavior<AiCarRescueCheckController>.Instance?.UnRegister(this);
	}

	public void DoUpdate(float deltaTime)
	{
		if (!SmartphonePrivateDriverUI.IsCurrentVehicle(base.gameObject) && vehicleComponent.GetCurrentSpeed() <= 0.1f)
		{
			_timeBeingStationary += deltaTime;
			if (_timeBeingStationary > 20f && (!vehicleRenderer.isVisible || _timeBeingStationary > 40f))
			{
				CoroutineUtility.RunAfterOneFrame(delegate
				{
					Manager.RemoveVehicle(base.gameObject);
					_timeBeingStationary = 0f;
				});
			}
		}
		else
		{
			_timeBeingStationary = 0f;
		}
	}
}
