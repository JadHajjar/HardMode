using Colossal.Entities;

using Game.City;
using Game.Simulation;
using Game.UI.InGame;

using HardMode.Domain;
using HardMode.Utility;

using HarmonyLib;

using System;

namespace HardMode.Patches
{
	[HarmonyPatch(typeof(CityServiceBudgetSystem), "GetMoneyDelta", new Type[] { })]
	public class ToolbarBottomUISystem_GetMoneyDelta
	{
		public static void Postfix(CityServiceBudgetSystem __instance, ref int __result)
		{
			var m_CitySystem = __instance.World.GetOrCreateSystemManaged<CitySystem>();
			var num = 0;
			for (var i = 0; i < (int)ExpenseSource.Count; i++)
			{
				num -= EconomyUtility.GetExpense((ExpenseSource)i, __instance.GetExpense((ExpenseSource)i));
			}

			for (var j = 0; j < (int)IncomeSource.Count; j++)
			{
				num += EconomyUtility.GetIncome((IncomeSource)j, __instance.GetIncome((IncomeSource)j));
			}

			if (__instance.EntityManager.TryGetBuffer<BufferedMoneyResource>(m_CitySystem.City, true, out var moneyBuffer) && moneyBuffer.Length > 0)
			{
				for (var i = 0; i < moneyBuffer.Length; i++)
				{
					var money = moneyBuffer[i];

					if (money.m_Value > 0)
					{
						num += EconomyUtility.GetIncome(IncomeSource.Count, money.m_Value);
					}
					else
					{
						num += EconomyUtility.GetExpense(ExpenseSource.Count, money.m_Value);
					}
				}
			}

			if (__instance.EntityManager.TryGetBuffer<PendingDemolitionCosts>(m_CitySystem.City, true, out var demoCostBuffer) && demoCostBuffer.Length > 0)
			{
				for (var i = 0; i < demoCostBuffer.Length; i++)
				{
					num -= demoCostBuffer[i].m_Value;
				}
			}

			__result = num / 24;
		}
	}
}
