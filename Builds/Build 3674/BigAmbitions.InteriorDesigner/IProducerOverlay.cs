using System.Collections.Generic;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public abstract class IProducerOverlay : MonoBehaviour
{
	public static int currentItemIndex;

	public static ItemController currentItemController;

	public List<GameObject> attachedObjects = new List<GameObject>();

	public abstract bool HasChanges();

	public abstract bool ShouldShow(ItemController itemController);

	public abstract void OnOpen(ItemController itemController);

	public abstract void ExecuteRevertibleAction();
}
