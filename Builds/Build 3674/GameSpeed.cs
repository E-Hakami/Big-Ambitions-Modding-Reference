using System;

[Serializable]
public class GameSpeed
{
	public bool paused;

	public TimeSpeed speed;

	public bool showPauseOverlay;

	public GameSpeed(bool paused, TimeSpeed speed, bool showPauseOverlay = true)
	{
		this.paused = paused;
		this.speed = speed;
		this.showPauseOverlay = showPauseOverlay;
	}
}
