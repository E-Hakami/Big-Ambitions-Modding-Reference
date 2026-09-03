using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Factories.Timeline;

[Serializable]
public class ShaderFloatBehavior : PlayableBehaviour
{
	public string materialPropertyName = "_Glossiness";

	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[NonSerialized]
	private float _initialFloat;

	[NonSerialized]
	private MaterialPropertyBlock _mpb;

	[NonSerialized]
	private int _propId;

	[NonSerialized]
	private Renderer _renderer;

	public override void OnGraphStart(Playable playable)
	{
		_initialFloat = curve.Evaluate(0f);
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		base.OnBehaviourPlay(playable, info);
		if (!string.IsNullOrEmpty(materialPropertyName))
		{
			_propId = Shader.PropertyToID(materialPropertyName);
			_mpb = new MaterialPropertyBlock();
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Renderer renderer = playerData as Renderer;
		if (!(renderer == null) && curve != null && !string.IsNullOrEmpty(materialPropertyName))
		{
			_renderer = renderer;
			renderer.GetPropertyBlock(_mpb);
			double duration = playable.GetDuration();
			if (!(duration <= 0.0))
			{
				float time = Mathf.Clamp01((float)(playable.GetTime() / duration));
				float b = curve.Evaluate(time);
				float value = Mathf.Lerp(_initialFloat, b, info.weight);
				_mpb.SetFloat(_propId, value);
				renderer.SetPropertyBlock(_mpb);
			}
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		base.OnBehaviourPause(playable, info);
		if (!(_renderer == null) && _mpb != null)
		{
			_renderer.GetPropertyBlock(_mpb);
			_mpb.SetFloat(_propId, _initialFloat);
			_renderer.SetPropertyBlock(_mpb);
		}
	}

	public override void OnGraphStop(Playable playable)
	{
		_initialFloat = curve.Evaluate(0f);
	}
}
