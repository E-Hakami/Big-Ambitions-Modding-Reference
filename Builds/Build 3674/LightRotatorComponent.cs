using UnityEngine;

public class LightRotatorComponent : MonoBehaviour
{
	public float rotationSpeed = 1f;

	private void Update()
	{
		base.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
	}
}
