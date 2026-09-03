using Helpers;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldDeliveryJobStart")]
public class TutorialPointerDataWorldDeliveryJobStart : TutorialPointerData
{
	private const float UpdateSqrDistance = 100f;

	private static readonly Vector3 PositionOffset = new Vector3(0f, 2f, 0f);

	public float maxDistance = 100f;

	private Vector3 _nearestStartPosition;

	private Vector3 _lastPlayerPosition;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.World;
	}

	public override bool ShouldBeEnabled()
	{
		CheckNearestStart();
		if (base.ShouldBeEnabled() && _nearestStartPosition != Vector3.zero)
		{
			return SaveGameManager.Current.currentPlayerMission == null;
		}
		return false;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		CheckNearestStart();
		if (_nearestStartPosition != Vector3.zero)
		{
			tutorialPointer.transform.position = _nearestStartPosition + PositionOffset;
		}
		Transform transform = tutorialPointer.transform;
		Vector3 forward = GameManager.GetMainCamera().transform.position - transform.position;
		forward.y = 0f;
		Quaternion rotation = Quaternion.LookRotation(forward);
		transform.rotation = rotation;
	}

	private void CheckNearestStart()
	{
		Vector3 position = PlayerHelper.GetPosition();
		if (!(_lastPlayerPosition != Vector3.zero) || !(Vector3.SqrMagnitude(position - _lastPlayerPosition) < 100f))
		{
			_lastPlayerPosition = position;
			DeliveryJobStartController nearest = DeliveryJobStartController.GetNearest(position, maxDistance);
			_nearestStartPosition = (nearest ? nearest.transform.position : Vector3.zero);
		}
	}
}
