using Colossal.UI.Binding;

using Game.City;
using Game.Simulation;
using Game.UI.InGame;

using HardMode.Systems;
using HardMode.Utility;

using HarmonyLib;

using System;

using Unity.Collections;
using Unity.Entities;

namespace HardMode.Patches
{
	[HarmonyPatch(typeof(TaxSystem), "GetEstimatedTaxAmount", new Type[] { typeof(TaxAreaType), typeof(TaxResultType), typeof(NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity>), typeof(BufferLookup<CityStatistic>), typeof(NativeArray<int>) })]
	public class TaxationUISystem_Patches
	{
		public static void Postfix(TaxAreaType areaType, TaxResultType resultType, ref int __result)
		{
			if (resultType != TaxResultType.Any)
			{
				return;
			}

			switch (areaType)
			{
				case TaxAreaType.Residential:
					__result = EconomyUtility.GetIncome(IncomeSource.TaxResidential, __result);
					break;
				case TaxAreaType.Commercial:
					__result = EconomyUtility.GetIncome(IncomeSource.TaxCommercial, __result);
					break;
				case TaxAreaType.Industrial:
					__result = EconomyUtility.GetIncome(IncomeSource.TaxIndustrial, __result);
					break;
				case TaxAreaType.Office:
					__result = EconomyUtility.GetIncome(IncomeSource.TaxOffice, __result);
					break;
			}
		}
	}

	[HarmonyPatch(typeof(ServiceBudgetUISystem))]
	public class ServiceBudgetUISystemPatches
	{
		private static readonly PatchedServiceBudgetUISystem _serviceBudgetUISystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PatchedServiceBudgetUISystem>();

		[HarmonyPrefix, HarmonyPatch("GetTotalBudget", new Type[] { typeof(Entity), typeof(DynamicBuffer<ServiceFee>) })]
		public static bool GetTotalBudget(Entity service, DynamicBuffer<ServiceFee> fees, ref int __result)
		{
			__result = _serviceBudgetUISystem.GetTotalBudget(service, fees);

			return false;
		}

		[HarmonyPrefix, HarmonyPatch("WriteServiceDetails", new Type[] { typeof(IJsonWriter), typeof(Entity) })]
		public static bool WriteServiceDetails(IJsonWriter writer, Entity serviceEntity, ServiceBudgetUISystem __instance)
		{
			_serviceBudgetUISystem.WriteServiceDetails(writer, serviceEntity, __instance);

			return true;
		}
	}
}
