using Extensions;
using UnityEngine;
using UnityEngine.Serialization;

public class ScreenVideoController : MonoBehaviour
{
	[SerializeField]
	private int materialIndex;

	[SerializeField]
	private float repeatX = 1f;

	[SerializeField]
	private float repeatY = 1f;

	[FormerlySerializedAs("renderer")]
	[SerializeField]
	private Renderer screenRenderer;

	private Color originalColor;

	private bool isPlaying;

	private static readonly int ExposureWeightID = Shader.PropertyToID("_EmissiveExposureWeight");

	private static readonly int EmissiveColorMapID = Shader.PropertyToID("_EmissiveColorMap");

	private static readonly int EmissiveColorMapSt = Shader.PropertyToID("_EmissiveColorMap_ST");

	private static MaterialPropertyBlock _propertyBlock;

	private VideoClipData _currentPlayingType;

	private float _nextUpdateTime;

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public void Play(VideoClipData.VideoType type)
	{
		isPlaying = true;
		screenRenderer.GetPropertyBlock(PropertyBlock, materialIndex);
		PropertyBlock.SetFloat(ExposureWeightID, 0f);
		PropertyBlock.SetVector(EmissiveColorMapSt, new Vector4(repeatX, repeatY, 0f, 0f));
		screenRenderer.SetPropertyBlock(PropertyBlock, materialIndex);
		_currentPlayingType = InstanceBehavior<GlobalReferences>.Instance.VideoClips[type].GetRandom();
		_nextUpdateTime = Time.time;
	}

	private void FixedUpdate()
	{
		if (isPlaying)
		{
			float time = Time.time;
			if (_nextUpdateTime <= time)
			{
				Texture2D texture2D = (_currentPlayingType.random ? _currentPlayingType.clip.GetRandom() : _currentPlayingType.clip[(int)(time / _currentPlayingType.speed.x % (float)_currentPlayingType.clip.Length)]);
				texture2D.wrapMode = TextureWrapMode.Repeat;
				screenRenderer.GetPropertyBlock(PropertyBlock, materialIndex);
				PropertyBlock.SetTexture(EmissiveColorMapID, texture2D);
				screenRenderer.SetPropertyBlock(PropertyBlock, materialIndex);
				_nextUpdateTime += _currentPlayingType.speed.RandomValue();
			}
		}
	}

	public void SetRenderTexture(RenderTexture texture)
	{
		isPlaying = false;
		screenRenderer.GetPropertyBlock(PropertyBlock, materialIndex);
		PropertyBlock.SetFloat(ExposureWeightID, 0f);
		PropertyBlock.SetTexture(EmissiveColorMapID, texture);
		screenRenderer.SetPropertyBlock(PropertyBlock, materialIndex);
	}

	public void Stop()
	{
		screenRenderer.GetPropertyBlock(PropertyBlock, materialIndex);
		PropertyBlock.Clear();
		screenRenderer.SetPropertyBlock(PropertyBlock, materialIndex);
		isPlaying = false;
	}
}
