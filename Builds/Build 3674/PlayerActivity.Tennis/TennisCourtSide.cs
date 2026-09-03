using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisCourtSide : MonoBehaviour
{
	private const float ServiceLineDistance = 6.5f;

	private const float NetAimMinDistance = 2f;

	public TennisCourt court;

	public TennisPlayer player;

	public MeshRenderer serviceAreaRenderer;

	public Bounds playerLocalBounds;

	public Bounds ballLocalBounds;

	public Bounds localServeLine;

	public Vector3 localServeReceivePosition;

	public bool IsInBallBounds(Vector3 ballPosition)
	{
		Vector3 vector = base.transform.InverseTransformPoint(ballPosition);
		if (vector.x > ballLocalBounds.min.x && vector.x < ballLocalBounds.max.x && vector.z > ballLocalBounds.min.z)
		{
			return vector.z < ballLocalBounds.max.z;
		}
		return false;
	}

	public Vector3 ClampToAimableBounds(Vector3 position)
	{
		Bounds bounds = ballLocalBounds;
		Vector3 position2 = base.transform.InverseTransformPoint(position);
		position2.x = Mathf.Clamp(position2.x, bounds.min.x, bounds.max.x);
		position2.z = Mathf.Clamp(position2.z, bounds.min.z, -2f);
		if (court.AwaitingServe && court.ServingSide != this)
		{
			position2.z = Mathf.Max(position2.z, -6.5f);
			position2.x = (court.IsServingOnRightSide ? Mathf.Max(position2.x, 0f) : Mathf.Min(position2.x, 0f));
		}
		return base.transform.TransformPoint(position2);
	}

	public Vector3 GetServiceAreaCenter()
	{
		Vector3 position = new Vector3(court.IsServingOnRightSide ? (ballLocalBounds.max.x / 2f) : (ballLocalBounds.min.x / 2f), 0f, -3.25f);
		return base.transform.TransformPoint(position);
	}

	public Vector3 GetAimableAreaCenter()
	{
		Vector3 position = new Vector3(0f, 0f, (ballLocalBounds.min.z - 2f) / 2f);
		return base.transform.TransformPoint(position);
	}

	public void UpdateServiceAreaSide()
	{
		Vector3 localPosition = serviceAreaRenderer.transform.localPosition;
		localPosition.x = Mathf.Abs(localPosition.x);
		if (!court.IsServingOnRightSide)
		{
			localPosition.x = 0f - localPosition.x;
		}
		serviceAreaRenderer.transform.localPosition = localPosition;
	}

	public void SetServiceAreaAlpha(float alpha)
	{
		if (alpha <= 0f)
		{
			if (serviceAreaRenderer.gameObject.activeSelf)
			{
				serviceAreaRenderer.gameObject.SetActive(value: false);
			}
			return;
		}
		if (!serviceAreaRenderer.gameObject.activeSelf)
		{
			serviceAreaRenderer.gameObject.SetActive(value: true);
		}
		Color color = serviceAreaRenderer.material.color;
		color.a = Mathf.Clamp01(alpha);
		serviceAreaRenderer.material.color = color;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = base.transform.localToWorldMatrix;
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(playerLocalBounds.center, playerLocalBounds.size);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(ballLocalBounds.center, ballLocalBounds.size);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(localServeLine.center, localServeLine.size);
	}
}
