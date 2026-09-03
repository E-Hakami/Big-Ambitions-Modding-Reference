using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class TryUseToilet : Action
{
	private const int DontThinkSoMinutes = 3;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	public float skipChance = 0.5f;

	public float washHandsChance = 0.9f;

	public bool failIfNotFound;

	private HygieneItemController _controller;

	private bool _reached;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private CharacterRunAnimation _characterRunAnimation;

	private readonly CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private Timestamp _waitUntil;

	private bool _skipped;

	private bool _wantsPrivacy;

	private bool _washHandsPhase;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
		_skipped = Random.value < skipChance;
	}

	public override TaskStatus OnUpdate()
	{
		if (_skipped)
		{
			_skipped = false;
			return TaskStatus.Success;
		}
		if (!_reached)
		{
			if (!_controller || _controller.Occupied)
			{
				if (_washHandsPhase)
				{
					_controller = FindSink(sharedCustomer.Value.tpc);
				}
				else
				{
					_wantsPrivacy = CustomerWantsStall();
					_controller = FindToilet(sharedCustomer.Value.tpc, _wantsPrivacy);
				}
				if (!_controller || !sharedCustomer.Value.tpc.navmeshAgent.isOnNavMesh || !_characterMoveToPosition.TryStartMovingToPosition(_controller.GetNavMeshTargetPosition()))
				{
					if (!failIfNotFound)
					{
						return TaskStatus.Success;
					}
					return TaskStatus.Failure;
				}
			}
			if (_characterMoveToPosition.HasPartialPath())
			{
				if (!failIfNotFound)
				{
					return TaskStatus.Success;
				}
				return TaskStatus.Failure;
			}
			if (!_characterMoveToPosition.HasReachedDestination())
			{
				return TaskStatus.Running;
			}
			if (!OnReached())
			{
				return TaskStatus.Success;
			}
		}
		if (_characterRunAnimation != null)
		{
			if (!_characterRunAnimation.IsAnimationFinished() || !_waitUntil.IsInThePast())
			{
				return TaskStatus.Running;
			}
		}
		else
		{
			if ((bool)_controller && _controller.Occupied && !_waitUntil.IsInThePast())
			{
				_controller.UpdateRotation(sharedCustomer.Value.tpc);
				return TaskStatus.Running;
			}
			if ((bool)_controller)
			{
				_controller.EndUse(sharedCustomer.Value.tpc);
				sharedCustomer.Value.tpc.ShowHandContent();
				BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _controller.ItemInstance);
				if (!_washHandsPhase && Random.value < washHandsChance)
				{
					_controller = null;
					Reset();
					_washHandsPhase = true;
					return TaskStatus.Running;
				}
			}
		}
		return TaskStatus.Success;
	}

	private bool OnReached()
	{
		_reached = true;
		_characterMoveToPosition.StopCheckingDestination();
		sharedCustomer.Value.tpc.HideHandContent();
		if (!_washHandsPhase && _wantsPrivacy && !_controller.Item.HasTag(TagRef.Itemtag.isprivacytoilet))
		{
			_characterRunAnimation = new CharacterRunAnimation();
			_characterRunAnimation.Init(sharedCustomer.Value.tpc);
			_characterRotateTowards.StartRotatingTowards(_controller.transform.position);
			_characterRunAnimation.StartRunningAnimation(AnimationType.IDontThinkSo);
			_characterShowEmojiExpression.StartShowingEmoji(CharacterEmojiName.CustomerDemandToiletStalls, 4f);
			_waitUntil = TimeHelper.Now();
			_waitUntil.AddMinutes(3f);
			return true;
		}
		_waitUntil = TimeHelper.Now();
		_waitUntil.AddMinutes(_controller.hygieneEnvironment.GetDefaultMinutes());
		return _controller.BeginUse(sharedCustomer.Value.tpc);
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	public static HygieneItemController FindToilet(ThirdPersonCharacter tpc, bool wantsPrivacy)
	{
		HygieneItemController hygieneItemController = null;
		HygieneItemController hygieneItemController2 = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		Vector3 position = tpc.transform.position;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (!allItemController || allItemController.Occupied || !(allItemController is ToiletController toiletController))
			{
				continue;
			}
			PlayerItemPurchaserSettings playerItemPurchaserSettings = toiletController.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled)
			{
				continue;
			}
			bool flag = !toiletController.Item.HasTag(TagRef.Itemtag.isprivacytoilet) & wantsPrivacy;
			if (flag && (bool)hygieneItemController)
			{
				continue;
			}
			float sqrMagnitude = (toiletController.transform.position - position).sqrMagnitude;
			if (!(sqrMagnitude > (flag ? num2 : num)))
			{
				if (flag)
				{
					num2 = sqrMagnitude;
					hygieneItemController2 = toiletController;
				}
				else
				{
					num = sqrMagnitude;
					hygieneItemController = toiletController;
				}
			}
		}
		return hygieneItemController ?? hygieneItemController2;
	}

	public static SinkController FindSink(ThirdPersonCharacter tpc)
	{
		SinkController result = null;
		float num = float.MaxValue;
		Vector3 position = tpc.transform.position;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (!allItemController || allItemController.Occupied || !(allItemController is SinkController sinkController))
			{
				continue;
			}
			PlayerItemPurchaserSettings playerItemPurchaserSettings = sinkController.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled)
			{
				float sqrMagnitude = (allItemController.transform.position - position).sqrMagnitude;
				if (!(sqrMagnitude > num))
				{
					num = sqrMagnitude;
					result = sinkController;
				}
			}
		}
		return result;
	}

	private bool CustomerWantsStall()
	{
		foreach (string customerDemandType in sharedCustomer.Value.order.customerDemandTypes)
		{
			if (customerDemandType == "ba:customerdemand_toiletprivacy")
			{
				return true;
			}
		}
		return false;
	}

	private void Reset()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StopRotating();
		_characterShowEmojiExpression.StopShowingEmoji();
		_characterRunAnimation = null;
		if ((bool)_controller)
		{
			_controller.EndUse(sharedCustomer.Value.tpc);
		}
		_controller = null;
		_reached = false;
		_skipped = Random.value < skipChance;
		_wantsPrivacy = false;
		_washHandsPhase = false;
	}
}
