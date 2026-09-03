using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/SelfServiceCustomerPool", fileName = "SelfServiceCustomerPool")]
public class SelfServiceCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.SelfService;
	}

	protected override string GetPrefabName()
	{
		return "Characters/SelfServiceCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.tpc.SetHandContent(null);
		base.ActionOnRelease(customer);
	}
}
