using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/FullServiceCustomerPool", fileName = "FullServiceCustomerPool")]
public class FullServiceCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.FullService;
	}

	protected override string GetPrefabName()
	{
		return "Characters/FullServiceCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.tpc.SetHandContent(null);
		customer.tpc.RemoveHandObject();
		base.ActionOnRelease(customer);
		customer.tpc.Reset();
	}
}
