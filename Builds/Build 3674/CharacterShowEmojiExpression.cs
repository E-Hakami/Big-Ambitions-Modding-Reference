using System.Collections;
using UnityEngine;

public class CharacterShowEmojiExpression
{
	private ThirdPersonCharacter _tpc;

	private MonoBehaviour _coroutineHolder;

	private bool _isShowingEmoji;

	private Coroutine _coroutine;

	public void Init(ThirdPersonCharacter tpc, MonoBehaviour coroutineHolder = null)
	{
		_tpc = tpc;
		_coroutineHolder = coroutineHolder ?? tpc;
	}

	public void StartShowingEmoji(CharacterEmojiName emojiName, float duration = 2f, ExpressionDataContainer expressionData = null)
	{
		_isShowingEmoji = true;
		_coroutine = _coroutineHolder.StartCoroutine(ShowEmojiCoroutine(emojiName, duration, expressionData));
	}

	private IEnumerator ShowEmojiCoroutine(CharacterEmojiName emojiName, float expressionDuration, ExpressionDataContainer expressionData)
	{
		object localizationArgs = expressionData?.GetLocalizationArgs();
		yield return _tpc.ShowExpression(emojiName, expressionDuration, localizationArgs);
		_isShowingEmoji = false;
	}

	public bool HasFinishedShowingEmoji()
	{
		return !_isShowingEmoji;
	}

	public void StopShowingEmoji()
	{
		if (_coroutine != null)
		{
			_coroutineHolder.StopCoroutine(_coroutine);
		}
		_isShowingEmoji = false;
	}
}
