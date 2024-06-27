using Game.City;
using Game.Simulation;

using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;

namespace HardMode.Utility
{
	internal static class EconomyUtility
	{
		public static int ConvertFromBufferedValueToMonthly(int bufferedValue)
		{
			return ConvertFromBufferedValueToTick(bufferedValue, 1);
		}

		public static int ConvertFromBufferedValueToHourly(int bufferedValue)
		{
			return ConvertFromBufferedValueToTick(bufferedValue) * 1024 / 24;
		}

		public static int ConvertFromBufferedValueToTick(int bufferedValue, int updatesPerMonth = 1024)
		{
			if (bufferedValue < 0)
			{
				return -math.min(-bufferedValue, 1000 + (((-5 * bufferedValue) + Mathf.RoundToInt(math.pow(bufferedValue / 100f, 2))) / updatesPerMonth));
			}

			return math.min(bufferedValue, 1000 + (((5 * bufferedValue) + Mathf.RoundToInt(math.pow(bufferedValue / 100f, 2))) / updatesPerMonth));
		}

		public static int GetExpense(ExpenseSource source, NativeArray<int> array)
		{
			return GetExpense(source, CityServiceBudgetSystem.GetExpense(source, array));
		}

		public static int GetExpense(ExpenseSource source, int value)
		{
			value = source switch
			{
				ExpenseSource.SubsidyResidential or ExpenseSource.SubsidyCommercial or ExpenseSource.SubsidyIndustrial or ExpenseSource.SubsidyOffice => value * 5 / 3,
				ExpenseSource.LoanInterest => value * 3 / 2,
				ExpenseSource.ImportElectricity => value * 5 / 4,
				ExpenseSource.ImportWater or ExpenseSource.ExportSewage => value * 6,
				ExpenseSource.Count => value * 8 / 6,
				ExpenseSource.ImportPoliceService or ExpenseSource.ImportAmbulanceService or ExpenseSource.ImportHearseService or ExpenseSource.ImportFireEngineService or ExpenseSource.ImportGarbageService => value * 2 / 5,
				ExpenseSource.MapTileUpkeep => value * 3 / 5,
				_ => value * 11 / 10
			};

			return Mathf.RoundToInt(value * Mod.Settings?.EconomyDifficulty switch
			{
				Domain.EconomyDifficulty.Easy => 0.85f,
				Domain.EconomyDifficulty.Medium => 0.90f,
				Domain.EconomyDifficulty.Hard => 1.00f,
				Domain.EconomyDifficulty.GoodLuck => 1.10f,
				_ => 1f,
			});
		}

		public static int GetIncome(IncomeSource source, NativeArray<int> array)
		{
			return GetIncome(source, CityServiceBudgetSystem.GetIncome(source, array));
		}

		public static int GetIncome(IncomeSource source, int value)
		{
			value = source switch
			{
				IncomeSource.TaxResidential or IncomeSource.TaxCommercial or IncomeSource.TaxIndustrial => value * 8 / 10,
				IncomeSource.TaxOffice => value * 16 / 13,
				IncomeSource.FeeElectricity => value * 4 / 5,
				IncomeSource.ExportElectricity => value * 2 / 5,
				IncomeSource.ExportWater or IncomeSource.FeeWater => value * 3,
				IncomeSource.FeeGarbage => value * 4 / 3,
				IncomeSource.FeeEducation => value * 5 / 3,
				IncomeSource.FeeHealthcare => value * 5,
				IncomeSource.Count => value / 3,
				_ => value
			};

			return Mathf.RoundToInt(value / Mod.Settings?.EconomyDifficulty switch
			{
				Domain.EconomyDifficulty.Easy => 0.85f,
				Domain.EconomyDifficulty.Medium => 0.9f,
				Domain.EconomyDifficulty.Hard => 0.95f,
				Domain.EconomyDifficulty.GoodLuck => 1f,
				_ => 1f,
			});
		}
	}
}
