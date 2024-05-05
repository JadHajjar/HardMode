namespace HardMode.Patches
{
	//[HarmonyPatch(typeof(MoneyBufferingSystem), "OnUpdate", new Type[] { })]
	//public class MoneyBufferingSystem_OnUpdate
	//{
	//	private const int kUpdatesPerDay = 1024;

	//	public static bool Prefix(MoneyBufferingSystem __instance)
	//	{
	//		var m_CityServiceBudgetSystem = __instance.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
	//		var m_CitySystem = __instance.World.GetOrCreateSystemManaged<CitySystem>();
	//		var num = 0;
	//		for (var i = 0; i < 9; i++)
	//		{
	//			num -= EconomyUtility.GetExpense((ExpenseSource)i, m_CityServiceBudgetSystem.GetExpense((ExpenseSource)i));
	//		}
	//		for (var j = 0; j < 14; j++)
	//		{
	//			num += EconomyUtility.GetIncome((IncomeSource)j, m_CityServiceBudgetSystem.GetIncome((IncomeSource)j));
	//		}

	//		var resourceBuffer = __instance.EntityManager.GetBuffer<Resources>(m_CitySystem.City, false);
	//		var moneyResource = EconomyUtils.GetResources(Resource.Money, resourceBuffer);

	//		EconomyUtils.SetResources(Resource.Money, resourceBuffer, 0);

	//		if (__instance.EntityManager.TryGetBuffer<BufferedMoneyResource>(m_CitySystem.City, false, out var moneyBuffer))
	//		{
	//			for (var i = 0; i < moneyBuffer.Length; i++)
	//			{
	//				var money = moneyBuffer[i];

	//				if (money.m_Value > 0)
	//				{
	//					num += EconomyUtility.GetIncome(IncomeSource.Count, money.m_Value);
	//				}
	//				else
	//				{
	//					num += EconomyUtility.GetExpense(ExpenseSource.Count, money.m_Value);
	//				}

	//				money.m_SimulationTick++;

	//				if (money.m_SimulationTick == kUpdatesPerDay)
	//				{
	//					moneyBuffer.RemoveAt(i);
	//					i--;
	//				}
	//				else
	//				{
	//					moneyBuffer[i] = money;
	//				}
	//			}

	//			if (moneyResource != 0)
	//			{
	//				moneyBuffer.Add(new BufferedMoneyResource { m_Value = moneyResource });
	//			}
	//		}

	//		if (__instance.EntityManager.TryGetBuffer<PendingDemolitionCosts>(m_CitySystem.City, true, out var demoCostBuffer))
	//		{
	//			for (var i = 0; i < demoCostBuffer.Length; i++)
	//			{
	//				var demoCost = demoCostBuffer[i];

	//				num -= demoCost.m_Value;

	//				demoCost.m_SimulationTick++;

	//				if (demoCost.m_SimulationTick == kUpdatesPerDay)
	//				{
	//					demoCostBuffer.RemoveAt(i);
	//					i--;
	//				}
	//				else
	//				{
	//					demoCostBuffer[i] = demoCost;
	//				}
	//			}
	//		}

	//		var playerMoney = __instance.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);

	//		playerMoney.Add(num / kUpdatesPerDay);

	//		__instance.EntityManager.SetComponentData(m_CitySystem.City, playerMoney);

	//		return false;
	//	}
	//}
}