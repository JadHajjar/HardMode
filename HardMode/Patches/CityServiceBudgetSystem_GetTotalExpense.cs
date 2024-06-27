using Colossal.Entities;

using Game.City;
using Game.Simulation;

using HardMode.Domain;
using HardMode.Utility;

using HarmonyLib;

using System;

using Unity.Collections;
using Unity.Entities;

namespace HardMode.Patches
{
	[HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetTotalExpenses), new Type[] { typeof(NativeArray<int>) })]
	public class CityServiceBudgetSystem_GetTotalExpense
	{
		private static readonly CitySystem m_CitySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CitySystem>();

		[HarmonyPrefix, HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetTotalExpenses), new Type[] { typeof(NativeArray<int>) })]
		public static bool Prefix(NativeArray<int> expenses, ref int __result)
		{
			var m_City = m_CitySystem.City;
			var total = 0;

			for (var i = 0; i < (int)ExpenseSource.Count; i++)
			{
				total -= EconomyUtility.GetExpense((ExpenseSource)i, expenses);
			}

			if (m_CitySystem.EntityManager.TryGetBuffer<BufferedMoneyResource>(m_City, true, out var moneyBuffer))
			{
				for (var i = 0; i < moneyBuffer.Length; i++)
				{
					var resource = moneyBuffer[i];
					if (resource.m_Value < 0)
					{
						total += EconomyUtility.GetExpense(ExpenseSource.Count, resource.m_Value);
					}
				}
			}

			if (m_CitySystem.EntityManager.TryGetBuffer<PendingDemolitionCosts>(m_City, true, out var demoCostsBuffer))
			{
				for (var i = 0; i < demoCostsBuffer.Length; i++)
				{
					var resource = demoCostsBuffer[i];
					total -= resource.m_Value;
				}
			}

			__result = total;

			return false;
		}
	}
}
