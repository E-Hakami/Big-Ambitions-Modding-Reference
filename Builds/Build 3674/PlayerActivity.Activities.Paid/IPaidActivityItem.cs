using UnityEngine;

namespace PlayerActivity.Activities.Paid;

public interface IPaidActivityItem
{
	Vector3 GetClosestNavMeshTargetPosition(Vector3 fromPosition);

	Transform GetPlayerWaitingTransform();

	void OnPlayerReached(PlayerController playerController, PaidActivity paidActivity);

	void OnPlayerReachedWaitingPosition();

	void OnFinish();
}
