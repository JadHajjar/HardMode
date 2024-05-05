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
				ExpenseSource.ImportElectricity => value * 3,
				ExpenseSource.ImportWater or ExpenseSource.ExportSewage => value * 6,
				ExpenseSource.Count => value * 7 / 5,
				_ => value
			};

			return Mathf.RoundToInt((Mod.Settings?.EconomyDifficulty) switch
			{
				Domain.EconomyDifficulty.Easy => value,
				Domain.EconomyDifficulty.Medium => value * 1.15f,
				Domain.EconomyDifficulty.Hard => value * 1.3f,
				Domain.EconomyDifficulty.GoodLuck => value * 1.45f,
				_ => value,
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
				IncomeSource.TaxResidential or IncomeSource.TaxCommercial or IncomeSource.TaxIndustrial or IncomeSource.TaxOffice => value * 4 / 5,
				IncomeSource.FeeElectricity => value * 9 / 5,
				IncomeSource.ExportElectricity => value * 2 / 5,
				IncomeSource.ExportWater or IncomeSource.FeeWater => value * 3,
				IncomeSource.FeeGarbage => value / 12,
				IncomeSource.FeeHealthcare => value * 8,
				IncomeSource.Count => value / 3,
				_ => value
			};

			return Mathf.RoundToInt((Mod.Settings?.EconomyDifficulty) switch
			{
				Domain.EconomyDifficulty.Easy => value / 0.95f,
				Domain.EconomyDifficulty.Medium => value / 1.1f,
				Domain.EconomyDifficulty.Hard => value / 1.25f,
				Domain.EconomyDifficulty.GoodLuck => value / 1.4f,
				_ => value,
			});
		}
	}
}
