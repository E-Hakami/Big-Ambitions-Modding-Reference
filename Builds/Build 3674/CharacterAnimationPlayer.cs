using BigAmbitions.Characters;
using UnityEngine;

public class CharacterAnimationPlayer : MonoBehaviour
{
	public float minTimeBetweenAnims;

	public float maxTimeBetweenAnims;

	public Animator characterAnimator;

	public AnimationType animationToPlay;

	private float _nextAnim;

	private float _animLength;

	private void Start()
	{
		_animLength = characterAnimator.GetAnimationLength(animationToPlay);
		_nextAnim = Random.Range(0f, maxTimeBetweenAnims);
		if (Random.Range(0f, 1f) < 0.15f)
		{
			characterAnimator.SetTrigger(animationToPlay);
			_nextAnim += _animLength;
		}
	}

	private void Update()
	{
		if (_nextAnim <= 0f)
		{
			_nextAnim = Random.Range(minTimeBetweenAnims, maxTimeBetweenAnims) + _animLength;
			characterAnimator.SetTrigger(animationToPlay);
		}
		_nextAnim -= Time.deltaTime;
	}
}
