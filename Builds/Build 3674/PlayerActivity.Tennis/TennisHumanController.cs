using BigAmbitions.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerActivity.Tennis;

public class TennisHumanController : TennisController
{
	public override Vector2 GetMovementInput()
	{
		return PlayerAction.Move.Vector();
	}

	public override Vector3 GetAimedPosition()
	{
		return base.Court.GetCursorAimedPosition();
	}

	public override bool GetServeInput()
	{
		if (PlayerAction.Click.Pressed())
		{
			return !EventSystem.current.IsPointerOverGameObject();
		}
		return false;
	}
}
