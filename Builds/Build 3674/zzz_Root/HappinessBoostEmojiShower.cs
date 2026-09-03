using UnityEngine;

public class HappinessBoostEmojiShower
{
	private readonly float _timeBetweenEmojis;

	private ThirdPersonCharacter _tpc;

	private float _elapsedTime;

	private bool _isEnabled;

	public HappinessBoostEmojiShower(ThirdPersonCharacter tpc, float timeBetweenEmojis)
	{
		_tpc = tpc;
		_timeBetweenEmojis = timeBetweenEmojis;
	}

	public void SetTpc(ThirdPersonCharacter tpc)
	{
		_tpc = tpc;
	}

	public void Enable()
	{
		_isEnabled = true;
		_elapsedTime = 0f;
	}

	public void Disable()
	{
		_isEnabled = false;
	}

	public void Update()
	{
		if (_isEnabled && Time.timeScale != 0f)
		{
			if (_elapsedTime >= _timeBetweenEmojis)
			{
				_tpc.EnqueuePlayerExpression(CharacterEmojiName.PlayerHappinessIncrease);
				_elapsedTime = 0f;
			}
			else
			{
				_elapsedTime += Time.unscaledDeltaTime;
			}
		}
	}
}
