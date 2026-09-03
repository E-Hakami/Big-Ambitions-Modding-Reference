using UnityEngine;

public class CasinoBoatWheelIndoor : MonoBehaviour
{
	public float rotationSpeed = 1f;

	private void Update()
	{
		base.transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
	}
}
