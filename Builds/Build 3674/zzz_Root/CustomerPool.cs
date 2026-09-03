using Extensions;
using Helpers;
using UnityEngine;

public class CustomerPool : Pool<Customer>
{
	private int _customerIndex;

	public virtual CustomerType GetCustomerType()
	{
		return CustomerType.None;
	}

	protected override string GetPrefabName()
	{
		return "";
	}

	public override void Dispose()
	{
		base.Dispose();
		_customerIndex = 0;
	}

	protected override Customer CreateFunc(Transform parent)
	{
		Vector3 navmeshSafePositionForNpcs = NavMeshHelper.GetNavmeshSafePositionForNpcs();
		Customer customer = PrefabHelper.CreatePrefab<Customer>(GetPrefabName(), navmeshSafePositionForNpcs, Quaternion.identity, parent);
		InitCustomer(customer);
		return customer;
	}

	private void InitCustomer(Customer customer)
	{
		_customerIndex++;
		customer.gameObject.name = inspectorGameObjectName + _customerIndex;
		customer.tpc.navmeshAgent.enabled = true;
		InitBehaviorTree(customer);
	}

	protected override void ActionOnGet(Customer customer)
	{
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.isSittingOn = null;
		customer.tpc.navmeshAgent.Warp(NavMeshHelper.GetNavmeshSafePositionForNpcs());
		customer.gameObject.SetActive(value: false);
		customer.behaviorTree.DisableBehavior();
		customer.UnsubscribeToGlobalEvents();
		customer.StopAllCoroutines();
		customer.tpc.StopAllCoroutines();
	}

	protected override void ActionOnDestroy(Customer customer)
	{
		if ((bool)customer)
		{
			Object.Destroy(customer.gameObject);
		}
	}

	private static void InitBehaviorTree(Customer customer)
	{
		customer.behaviorTree.ExternalBehavior.SetVariableValue("IsInit", true);
		customer.behaviorTree.EnableBehavior();
		customer.behaviorTree.ExternalBehavior.SetVariableValue("IsInit", false);
		customer.behaviorTree.DisableBehavior();
	}
}
