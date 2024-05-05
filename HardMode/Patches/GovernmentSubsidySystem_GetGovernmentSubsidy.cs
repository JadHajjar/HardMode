using Game.Simulation;

using HarmonyLib;

using System;

using Unity.Mathematics;

using UnityEngine;

namespace HardMode.Patches
{
	[HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetGovernmentSubsidy), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
	public class GovernmentSubsidySystem_GetGovernmentSubsidy
	{
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
		public static void Postfix(int population, int moneyDelta, int expenses, int loanInterest, ref int __result)
		{
			switch (Mod.Settings?.EconomyDifficulty)
			{
				case Domain.EconomyDifficulty.Easy:
					population = population * 2 / 3;
					break;
				case Domain.EconomyDifficulty.Hard:
					population = population * 3 / 2;
					break;
				case Domain.EconomyDifficulty.GoodLuck:
					population = population * 5 / 2;
					break;
			}

			var earlyGameCrashPreventionValue = math.max(0, (math.pow(-population / 1500f, 3f) + 1) * math.max(0, -moneyDelta / 2));
			var steadyEarlySubsidyValue = 75_000 + (loanInterest / 2) + math.pow(-population / 500f, 3f);
			var linearExpenseSubsidyValue = (10_000 - population) / 25_000f * math.max(0, -expenses) * math.min(1, -750_000f / math.select(expenses, 1, expenses == 0));
			var balanceDeviationMultiplier = math.abs((float)expenses / math.select(moneyDelta + expenses, 1, moneyDelta + expenses == 0));
			var randomDeviationValue = 3000 * math.cos(0.01f * population);

			var subsidies = Mod.Settings?.EconomyDifficulty switch
			{
				Domain.EconomyDifficulty.Easy
					=> earlyGameCrashPreventionValue + (steadyEarlySubsidyValue * 3 / 2) + (linearExpenseSubsidyValue * 3 / 2) + randomDeviationValue,

				Domain.EconomyDifficulty.Medium
					=> earlyGameCrashPreventionValue + steadyEarlySubsidyValue + linearExpenseSubsidyValue + randomDeviationValue,

				Domain.EconomyDifficulty.Hard
					=> earlyGameCrashPreventionValue + (steadyEarlySubsidyValue * 2 / 3) + (linearExpenseSubsidyValue * 2 / 3) + randomDeviationValue,

				Domain.EconomyDifficulty.GoodLuck
					=> (earlyGameCrashPreventionValue + steadyEarlySubsidyValue) * 2 / 5,

				_ => 0
			};

			subsidies /= balanceDeviationMultiplier;

			__result = math.clamp(Mathf.RoundToInt(subsidies), 0, -expenses);
		}
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
	}
}