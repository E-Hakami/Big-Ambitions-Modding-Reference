using UnityEngine;

namespace Controllers;

public interface ICarnivalNpcItem
{
	void OnActivate();

	void OnDeactivate();

	bool CanPlaceNpc();

	void PlaceNpcInstantly(CarnivalPedestrian carnivalPedestrian);

	bool TryEnqueueNpc(CarnivalPedestrian carnivalPedestrian);

	int GetWaitingPositionIndex();

	Vector3 GetWaitingPositionFromIndex(int index);

	Quaternion GetWaitingRotationFromIndex(int index);

	Vector3 GetExitPosition(Vector3 fromPosition = default(Vector3));
}
