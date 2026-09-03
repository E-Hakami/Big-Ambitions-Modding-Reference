using System;

public static class UiSoundHelper
{
	public static Action<UiSound, bool> playSound;

	public static void Play(UiSound type, bool randomPitch = false)
	{
		playSound?.Invoke(type, randomPitch);
	}
}
