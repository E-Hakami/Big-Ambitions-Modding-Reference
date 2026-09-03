using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/CasinoCustomerPool", fileName = "CasinoCustomerPool")]
public class CasinoCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.Casino;
	}

	protected override string GetPrefabName()
	{
		return "Characters/CasinoCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.RemoveDrink();
		((CasinoCustomer)customer).lastAction = CasinoRandomAction.None;
		base.ActionOnRelease(customer);
		customer.tpc.Reset();
	}
}
