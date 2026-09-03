using UnityEngine;

namespace Controllers;

public class NpcSpawnerItem : MonoBehaviour
{
	public Transform spawnPoint;

	[SerializeField]
	private EntityController attachedEntityController;

	protected bool occupied;

	private ThirdPersonCharacter _spawnedTpc;

	private ItemController _itemController;

	public virtual void OnNpcSpawn(BaseHuman baseHuman)
	{
		baseHuman.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
		occupied = true;
		if ((object)_itemController == null)
		{
			_itemController = GetComponent<ItemController>();
		}
		_spawnedTpc = baseHuman as ThirdPersonCharacter;
		_spawnedTpc?.SetItemIKTargets(_itemController, smooth: true);
		if (attachedEntityController != null)
		{
			attachedEntityController.Occupied = true;
		}
	}

	public virtual void OnNpcDespawn()
	{
		_spawnedTpc?.SetItemIKTargets(null, smooth: true);
		_spawnedTpc = null;
		occupied = false;
		if (attachedEntityController != null)
		{
			attachedEntityController.Occupied = false;
		}
	}

	public virtual void UpdateItem()
	{
	}
}
