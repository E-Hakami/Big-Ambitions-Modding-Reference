using UnityEngine;

public class AudioListenerPositioner : MonoBehaviour
{
	public static AudioListenerPositioner Instance { get; private set; }

	public static Vector3 GetAudioListenerPosition()
	{
		if (InstanceBehavior<GameManager>.Instance?.playerController == null)
		{
			return GameManager.GetMainCamera().transform.position;
		}
		if (!CityMap.IsOpen)
		{
			return InstanceBehavior<GameManager>.Instance.playerController.transform.position;
		}
		return GameManager.GetMainCamera().transform.position;
	}

	private void Awake()
	{
		Instance = this;
	}

	private void LateUpdate()
	{
		base.transform.rotation = GameManager.GetMainCamera().transform.rotation;
		base.transform.position = GetAudioListenerPosition();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.forward * 2f);
		Gizmos.color = Color.white;
	}
}
