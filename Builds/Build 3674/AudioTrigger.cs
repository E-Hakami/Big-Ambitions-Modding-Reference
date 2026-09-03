using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioTrigger : MonoBehaviour
{
	public string key;

	public float time;

	public bool setOnStartup;

	[ShowIf("setOnStartup")]
	public float startValue;

	[Tooltip("X: Exit, Y: Enter")]
	[FormerlySerializedAs("minMax")]
	public Vector2 exitEnterValue = new Vector2(0f, 1f);

	public bool clearOnExit = true;

	private TweenerCore<float, float, FloatOptions> _tweener;

	private void Start()
	{
		if (setOnStartup)
		{
			InstanceBehavior<SfxManager>.Instance.audioMixer.SetFloat(key, startValue);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_tweener.Kill();
			_tweener = InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat(key, exitEnterValue.y, time).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.CompareTag("Player"))
		{
			return;
		}
		_tweener.Kill();
		_tweener = InstanceBehavior<SfxManager>.Instance.audioMixer.DOSetFloat(key, exitEnterValue.x, time).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		if (clearOnExit)
		{
			_tweener.OnComplete(delegate
			{
				InstanceBehavior<SfxManager>.Instance.audioMixer.ClearFloat(key);
			});
		}
	}
}
