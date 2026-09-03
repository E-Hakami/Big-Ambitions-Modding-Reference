using System;
using System.Collections.Generic;
using AI;
using BigAmbitions.Characters;
using Extensions;
using UnityEngine;

namespace Entities;

public class StationaryAiBehavior : MonoBehaviour
{
	private const float ChanceOfStartingWithAnimation = 0.15f;

	private const float ProbabilityToUsePermanentAnim = 0.6f;

	public StationaryAiData data = new StationaryAiData();

	[SerializeField]
	private BaseHuman human;

	[HideInInspector]
	public Transform chair;

	private readonly List<int> _animationsAux = new List<int>();

	private readonly Dictionary<PermanentAnimationType, AnimationType> _combinedAnimations = new Dictionary<PermanentAnimationType, AnimationType>();

	private bool _updateAnimations;

	private float _nextAnimTime;

	private int _currentAnim;

	private PermanentAnimationType _permanentAnimation;

	private UmbrellaHandler _umbrellaHandler;

	private PermanentAnimationType[] _permanentAnimations;

	private bool _isPlayingCombinedAnimation;

	private bool _enabled;

	public void Initialize(bool initAppearance = true, bool initAnimations = true)
	{
		for (int i = 0; i < data.animations.Length; i++)
		{
			_animationsAux.Add(i);
		}
		if (chair != null)
		{
			human.SitOnChair(chair);
		}
		else
		{
			base.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		}
		if (initAppearance)
		{
			InitAppearance();
		}
		InitializeUmbrellaHandler();
		data.usePermanentAnim = IsThereAnyPermanentAnimation() && 0.6f.Probability();
		if (data.usePermanentAnim)
		{
			InitPermanentAnimations();
		}
		if (initAnimations)
		{
			if (data.usePermanentAnim)
			{
				SetPermanentAnimation(GetRandomPermanentAnimation());
			}
			else
			{
				InitAnimations();
			}
		}
	}

	private bool IsThereAnyPermanentAnimation()
	{
		PermanentAnimationType[] permanentAnims = data.permanentAnims;
		if (permanentAnims != null && permanentAnims.Length != 0)
		{
			return true;
		}
		CombinedAnimation[] combinedAnimations = data.combinedAnimations;
		if (combinedAnimations == null)
		{
			return false;
		}
		return combinedAnimations.Length != 0;
	}

	private void InitPermanentAnimations()
	{
		_permanentAnimations = new PermanentAnimationType[(data.permanentAnims.Length + data.combinedAnimations?.Length).GetValueOrDefault()];
		if (data.combinedAnimations != null)
		{
			Array.Copy(data.permanentAnims, _permanentAnimations, data.permanentAnims.Length);
			int num = data.permanentAnims.Length;
			CombinedAnimation[] combinedAnimations = data.combinedAnimations;
			foreach (CombinedAnimation combinedAnimation in combinedAnimations)
			{
				_permanentAnimations[num++] = combinedAnimation.permanentAnimation;
				_combinedAnimations.Add(combinedAnimation.permanentAnimation, combinedAnimation.animation);
			}
		}
	}

	private PermanentAnimationType GetRandomPermanentAnimation()
	{
		return _permanentAnimations.GetRandom();
	}

	private void InitializeUmbrellaHandler()
	{
		bool hasUmbrella = !chair && !BuildingManager.IsInsideBuilding && human.HasUmbrella();
		_umbrellaHandler = new UmbrellaHandler(human, hasUmbrella, OnUmbrellaAdded, OnUmbrellaRemoved);
	}

	private void OnUmbrellaRemoved()
	{
		if (data.usePermanentAnim)
		{
			SetPermanentAnimation(GetRandomPermanentAnimation());
		}
	}

	private void OnUmbrellaAdded()
	{
		ResetPermanentAnimation();
	}

	private void ResetPermanentAnimation()
	{
		if (data.usePermanentAnim)
		{
			human.animator.SetBool(_permanentAnimation, state: false);
			_isPlayingCombinedAnimation = false;
		}
	}

	private void InitAppearance()
	{
		human.appearanceSetter.skipMeshMerging = data.skipMeshMerging;
		human.appearanceSetter.SetRandomAge();
		human.appearanceSetter.SetRandomAppearance(data.appearanceTags);
	}

	private void InitAnimations()
	{
		_updateAnimations = data.animations.Length != 0;
		if (_updateAnimations)
		{
			bool flag = 0.15f.Probability();
			_currentAnim = (flag ? UnityEngine.Random.Range(0, data.animations.Length) : (-1));
			_nextAnimTime = Time.time + UnityEngine.Random.Range(0f, data.maxTimeBetweenAnims);
			if (flag)
			{
				float animationLength = human.animator.GetAnimationLength(data.animations[_currentAnim]);
				human.animator.SetTrigger(data.animations[_currentAnim]);
				_nextAnimTime += animationLength;
			}
		}
	}

	private void SetPermanentAnimation(PermanentAnimationType anim)
	{
		_permanentAnimation = anim;
		_isPlayingCombinedAnimation = _combinedAnimations.ContainsKey(anim);
		human.animator.SetBool(anim);
		string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(anim);
		human.AddHandObject(handObjectNameFromPermanentAnimationType);
	}

	private void Update()
	{
		if (!_enabled)
		{
			return;
		}
		if (_umbrellaHandler != null)
		{
			_umbrellaHandler.Update();
			if (_umbrellaHandler.IsHoldingUmbrella())
			{
				return;
			}
		}
		if (data.usePermanentAnim)
		{
			if (_isPlayingCombinedAnimation && _nextAnimTime <= Time.time)
			{
				float num = human.animator.RunAnimationLength(_combinedAnimations[_permanentAnimation]);
				_nextAnimTime = Time.time + num + UnityEngine.Random.Range(3f, 6f);
			}
		}
		else if (_updateAnimations && _nextAnimTime <= Time.time)
		{
			HandleNextAnimation();
		}
	}

	private void HandleNextAnimation()
	{
		if (data.animations.Length > 1)
		{
			SelectRandomAnimation();
		}
		else
		{
			_currentAnim = 0;
		}
		float animationLength = human.animator.GetAnimationLength(data.animations[_currentAnim]);
		_nextAnimTime = Time.time + UnityEngine.Random.Range(data.minTimeBetweenAnims, data.maxTimeBetweenAnims) + animationLength;
		human.animator.SetTrigger(data.animations[_currentAnim]);
	}

	private void SelectRandomAnimation()
	{
		if (_currentAnim == -1)
		{
			_currentAnim = UnityEngine.Random.Range(0, data.animations.Length);
			return;
		}
		_animationsAux.Remove(_currentAnim);
		int random = _animationsAux.GetRandom();
		_animationsAux.Add(_currentAnim);
		_currentAnim = random;
	}

	public void Disable()
	{
		_umbrellaHandler?.ForceRemoveUmbrella();
		if (human != null)
		{
			human.RemoveHandObject();
			ResetPermanentAnimation();
		}
		_enabled = false;
	}

	public void Enable()
	{
		_enabled = true;
		_umbrellaHandler?.OnEnable();
		if (data.usePermanentAnim)
		{
			SetPermanentAnimation(GetRandomPermanentAnimation());
		}
		else
		{
			InitAnimations();
		}
	}
}
