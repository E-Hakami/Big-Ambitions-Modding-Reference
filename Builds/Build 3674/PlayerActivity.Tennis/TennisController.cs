using UnityEngine;

namespace PlayerActivity.Tennis;

public abstract class TennisController : MonoBehaviour
{
	protected TennisPlayer player;

	protected TennisCourt Court => player.Court;

	protected TennisCourtSide CourtSide => player.CourtSide;

	protected TennisBall Ball => player.Ball;

	private void Awake()
	{
		player = GetComponent<TennisPlayer>();
	}

	public abstract Vector2 GetMovementInput();

	public abstract Vector3 GetAimedPosition();

	public abstract bool GetServeInput();
}
