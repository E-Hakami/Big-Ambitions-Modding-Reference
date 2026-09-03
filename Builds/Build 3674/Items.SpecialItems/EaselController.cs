using NaughtyAttributes;
using UnityEngine;

namespace Items.SpecialItems;

public class EaselController : MonoBehaviour
{
	private static readonly int PictureNumberId = Shader.PropertyToID("_PictureNumber");

	private static readonly int PictureProgressId = Shader.PropertyToID("_PictureProgress");

	[Header("Shader")]
	[SerializeField]
	private int maxPictureIndex;

	[SerializeField]
	private int maxPictureProgressStep;

	[SerializeField]
	private Renderer pictureRenderer;

	[SerializeField]
	private int pictureMaterialIndex;

	[Header("Settings")]
	[SerializeField]
	[MinMaxSlider(0f, 20f)]
	private Vector2 progressStepRangeInSeconds;

	[SerializeField]
	private float resetStepTimeInSeconds;

	[SerializeField]
	private float finishedPaintingTimeInSeconds;

	private bool _hasPictureNumber;

	private bool _isResetting;

	private float _nextProgressTime;

	private int _pictureNumber;

	private int _pictureProgress;

	private MaterialPropertyBlock _picturePropertyBlock;

	private void Awake()
	{
		_picturePropertyBlock = new MaterialPropertyBlock();
	}

	private void Start()
	{
		StartNewPainting();
	}

	private void Update()
	{
		if (!(Time.time < _nextProgressTime))
		{
			if (_isResetting)
			{
				ResetProgressStep();
			}
			else
			{
				ProgressPaintingStep();
			}
		}
	}

	private void SetPictureProgress(int pictureProgress)
	{
		SetPictureShaderProperties(_pictureNumber, pictureProgress);
	}

	private void SetPictureShaderProperties(int pictureNumber, int pictureProgress)
	{
		_pictureNumber = pictureNumber;
		_pictureProgress = Mathf.Clamp(pictureProgress, 0, maxPictureProgressStep);
		_hasPictureNumber = true;
		if ((bool)pictureRenderer)
		{
			if (_picturePropertyBlock == null)
			{
				_picturePropertyBlock = new MaterialPropertyBlock();
			}
			pictureRenderer.GetPropertyBlock(_picturePropertyBlock, pictureMaterialIndex);
			_picturePropertyBlock.SetInt(PictureNumberId, pictureNumber);
			_picturePropertyBlock.SetFloat(PictureProgressId, _pictureProgress);
			pictureRenderer.SetPropertyBlock(_picturePropertyBlock, pictureMaterialIndex);
		}
	}

	private void StartNewPainting()
	{
		_isResetting = false;
		int num = maxPictureProgressStep;
		SetPictureShaderProperties(GetRandomPictureNumber(), Random.Range(0, num + 1));
		_nextProgressTime = Time.time + GetRandomProgressStepTime();
	}

	private void ProgressPaintingStep()
	{
		if (_pictureProgress >= maxPictureProgressStep)
		{
			_isResetting = true;
			_nextProgressTime = Time.time + finishedPaintingTimeInSeconds;
		}
		else
		{
			SetPictureProgress(_pictureProgress + 1);
			_nextProgressTime = Time.time + GetRandomProgressStepTime();
		}
	}

	private void ResetProgressStep()
	{
		if (_pictureProgress <= 0)
		{
			StartNewPainting();
			return;
		}
		SetPictureProgress(_pictureProgress - 1);
		_nextProgressTime = Time.time + Mathf.Max(0f, resetStepTimeInSeconds);
	}

	private int GetRandomPictureNumber()
	{
		int num = Mathf.Max(0, maxPictureIndex);
		if (num == 0)
		{
			return 0;
		}
		int num2 = Random.Range(0, num + 1);
		if (!_hasPictureNumber || num2 != _pictureNumber)
		{
			return num2;
		}
		return (num2 + Random.Range(1, num + 1)) % (num + 1);
	}

	private float GetRandomProgressStepTime()
	{
		return Random.Range(progressStepRangeInSeconds.x, progressStepRangeInSeconds.y);
	}
}
