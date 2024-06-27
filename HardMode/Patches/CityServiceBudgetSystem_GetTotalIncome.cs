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
	[HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetTotalIncome), new Type[] { typeof(NativeArray<int>) })]
	public class CityServiceBudgetSystem_GetTotalIncome
	{
		private static readonly CitySystem m_CitySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CitySystem>();

		public static bool Prefix(NativeArray<int> income, ref int __result)
		{
			var m_City = m_CitySystem.City;
			var total = 0;

			for (var j = 0; j < (int)IncomeSource.Count; j++)
			{
				total += EconomyUtility.GetIncome((IncomeSource)j, income);
			}

			if (m_CitySystem.EntityManager.TryGetBuffer<BufferedMoneyResource>(m_City, true, out var moneyBuffer))
			{
				for (var i = 0; i < moneyBuffer.Length; i++)
				{
					var resource = moneyBuffer[i];
					if (resource.m_Value > 0)
					{
						total += EconomyUtility.GetIncome(IncomeSource.Count, resource.m_Value);
					}
				}
			}

			__result = total;

			return false;
		}
	}
}
