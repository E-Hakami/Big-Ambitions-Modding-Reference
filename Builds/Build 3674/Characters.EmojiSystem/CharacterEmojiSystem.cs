using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Player.HUD.ItemWarningIcons;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace Characters.EmojiSystem;

public class CharacterEmojiSystem : MonoBehaviour
{
	private const string AddressableLabel = "CharacterEmojis";

	[SerializeField]
	private GameObject characterEmojiWithTextPrefab;

	[SerializeField]
	private GameObject characterEmojiPrefab;

	public static ObjectPool<CharacterEmojiExpression> characterEmojiWithTextPool;

	public static ObjectPool<CharacterEmojiExpression> characterEmojisPool;

	private static readonly Dictionary<CharacterEmojiName, CharacterEmoji> CharacterEmojis = new Dictionary<CharacterEmojiName, CharacterEmoji>();

	private static CharacterEmoji[] _characterEmojisArray;

	private static bool _initialized;

	private int _poolIndex;

	public void Awake()
	{
		if (!_initialized)
		{
			Dictionary<CharacterEmojiName, CharacterEmoji> characterEmojis = CharacterEmojis;
			if (characterEmojis == null || characterEmojis.Count < 1)
			{
				PopulateDictionary();
			}
			CreatePools();
			_initialized = true;
		}
	}

	private static void PopulateDictionary()
	{
		_characterEmojisArray = Addressables.LoadAssetsAsync<CharacterEmoji>("CharacterEmojis", null).WaitForCompletion().ToArray();
		CharacterEmojis.EnsureCapacity(_characterEmojisArray.Length);
		CharacterEmoji[] characterEmojisArray = _characterEmojisArray;
		foreach (CharacterEmoji characterEmoji in characterEmojisArray)
		{
			CharacterEmojis.Add(characterEmoji.characterEmojiName, characterEmoji);
		}
	}

	private void CreatePools()
	{
		characterEmojiWithTextPool = new ObjectPool<CharacterEmojiExpression>(GetNewCharacterEmojiWithText, delegate(CharacterEmojiExpression obj)
		{
			obj.gameObject.SetActive(value: true);
		}, delegate(CharacterEmojiExpression obj)
		{
			obj.gameObject.SetActive(value: false);
		}, delegate(CharacterEmojiExpression obj)
		{
			if (obj != null)
			{
				UnityEngine.Object.Destroy(obj.gameObject);
			}
		}, collectionCheck: false, 10, 30);
		characterEmojisPool = new ObjectPool<CharacterEmojiExpression>(GetNewCharacterEmoji, delegate(CharacterEmojiExpression obj)
		{
			obj.gameObject.SetActive(value: true);
		}, delegate(CharacterEmojiExpression obj)
		{
			obj.gameObject.SetActive(value: false);
		}, delegate(CharacterEmojiExpression obj)
		{
			if (obj != null)
			{
				UnityEngine.Object.Destroy(obj.gameObject);
			}
		}, collectionCheck: false, 3, 5);
	}

	public static CharacterEmoji GetCharacterEmojiByName(CharacterEmojiName emoji)
	{
		Dictionary<CharacterEmojiName, CharacterEmoji> characterEmojis = CharacterEmojis;
		if (characterEmojis == null || characterEmojis.Count < 1)
		{
			PopulateDictionary();
		}
		return CharacterEmojis.GetValueOrDefault(emoji);
	}

	private CharacterEmojiExpression GetNewCharacterEmojiWithText()
	{
		GameObject obj = UnityEngine.Object.Instantiate(characterEmojiWithTextPrefab, InstanceBehavior<ItemWarningIconManager>.Instance.emojiParent);
		_poolIndex++;
		obj.name = "characterExpression" + _poolIndex;
		return obj.GetComponent<CharacterEmojiExpression>();
	}

	private CharacterEmojiExpression GetNewCharacterEmoji()
	{
		return UnityEngine.Object.Instantiate(characterEmojiPrefab, InstanceBehavior<ItemWarningIconManager>.Instance.emojiParent).GetComponent<CharacterEmojiExpression>();
	}

	public static IEnumerator ShowEmoji(Transform target, CharacterEmojiName characterEmojiName, bool showText, float secondsToShow, object localizationArgs = null, Action<CharacterEmojiExpression> callback = null)
	{
		if (_initialized)
		{
			CharacterEmojiExpression characterEmojiExpression = GetCharacterEmojiExpression(showText);
			CharacterEmoji characterEmojiData = CharacterEmojis[characterEmojiName];
			InitCharacterEmojiExpression(target, showText, localizationArgs, characterEmojiExpression, characterEmojiData);
			callback?.Invoke(characterEmojiExpression);
			yield return characterEmojiExpression.Show(secondsToShow, !showText);
			if ((bool)characterEmojiExpression && (bool)characterEmojiExpression.inWorldTarget)
			{
				characterEmojiExpression.Release();
			}
		}
	}

	private static void InitCharacterEmojiExpression(Transform target, bool showText, object localizationArgs, CharacterEmojiExpression characterEmojiExpression, CharacterEmoji characterEmojiData)
	{
		characterEmojiExpression.SetTarget(target);
		characterEmojiExpression.SetImages(characterEmojiData.background, characterEmojiData.icon, characterEmojiData.modifierIcon);
		if (showText)
		{
			characterEmojiExpression.SetText(characterEmojiData.localizationKey, localizationArgs);
		}
	}

	private static CharacterEmojiExpression GetCharacterEmojiExpression(bool showText)
	{
		if (!showText)
		{
			return characterEmojisPool.Get();
		}
		return characterEmojiWithTextPool.Get();
	}

	private void OnDestroy()
	{
		characterEmojisPool.Dispose();
		characterEmojiWithTextPool.Dispose();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		CharacterEmojis.Clear();
		_initialized = false;
	}
}
