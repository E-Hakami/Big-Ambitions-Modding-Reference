using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/ThirdPersonCharacterPool")]
public class ThirdPersonCharacterPool : Pool<ThirdPersonCharacter>
{
	[SerializeField]
	private bool isPlayerAgentType = true;

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/HumanDefinitionLow";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override ThirdPersonCharacter CreateFunc(Transform parent)
	{
		ThirdPersonCharacter thirdPersonCharacter = PrefabHelper.CreatePrefab<ThirdPersonCharacter>(GetPrefabName(), parent);
		if (isPlayerAgentType)
		{
			thirdPersonCharacter.navmeshAgent.agentTypeID = 1479372276;
		}
		_poolIndex++;
		thirdPersonCharacter.name = inspectorGameObjectName + _poolIndex;
		return thirdPersonCharacter;
	}

	protected override void ActionOnGet(ThirdPersonCharacter human)
	{
		human.gameObject.SetActive(value: true);
	}

	protected override void ActionOnRelease(ThirdPersonCharacter human)
	{
		human.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(ThirdPersonCharacter human)
	{
		if ((bool)human)
		{
			Object.Destroy(human.gameObject);
		}
	}
}
