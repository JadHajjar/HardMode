using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;

using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;

using HardMode.Domain;
using HardMode.Utility;

using System;

using Unity.Entities;

namespace HardMode.Systems
{
	public partial class CustomBudgetUISystem : UISystemBase
	{
		private PrefabSystem m_PrefabSystem;
		private BudgetUISystem m_BudgetUISystem;
		private CityServiceBudgetSystem m_CityServiceBudgetSystem;

		private EntityQuery m_ConfigQuery;

		private GetterValueBinding<int> m_TotalIncomeBinding;

		private GetterValueBinding<int> m_TotalExpensesBinding;

		private RawValueBinding m_IncomeItemsBinding;

		private RawValueBinding m_IncomeValuesBinding;

		private RawValueBinding m_ExpenseItemsBinding;

		private RawValueBinding m_ExpenseValuesBinding;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
			m_BudgetUISystem = World.GetOrCreateSystemManaged<BudgetUISystem>();
			m_CityServiceBudgetSystem = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
			m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIEconomyConfigurationData>());
			AddBinding(m_TotalIncomeBinding = new GetterValueBinding<int>("budget", "totalIncome", GetTotalIncome));
			AddBinding(m_TotalExpensesBinding = new GetterValueBinding<int>("budget", "totalExpenses", GetTotalExpenses));
			AddBinding(m_IncomeItemsBinding = new RawValueBinding("budget", "incomeItems", BindIncomeItems));
			AddBinding(m_IncomeValuesBinding = new RawValueBinding("budget", "incomeValues", BindIncomeValues));
			AddBinding(m_ExpenseItemsBinding = new RawValueBinding("budget", "expenseItems", BindExpenseItems));
			AddBinding(m_ExpenseValuesBinding = new RawValueBinding("budget", "expenseValues", BindExpenseValues));
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);
			m_BudgetUISystem.Enabled = false;
		}

		protected override void OnUpdate()
		{
			//m_BudgetUISystem.Enabled = false;

			m_TotalIncomeBinding.Update();
			m_TotalExpensesBinding.Update();
			m_IncomeValuesBinding.Update();
			m_ExpenseValuesBinding.Update();
		}

		public int GetTotalIncome()
		{
			return m_CityServiceBudgetSystem.GetTotalIncome();
		}

		private int GetTotalExpenses()
		{
			return m_CityServiceBudgetSystem.GetTotalExpenses();
		}

		private void BindIncomeItems(IJsonWriter writer)
		{
			var config = GetConfig();
			writer.ArrayBegin(config.m_IncomeItems.Length);
			var incomeItems = config.m_IncomeItems;
			foreach (var budgetItem in incomeItems)
			{
				writer.TypeBegin("Game.UI.InGame.BudgetItem");
				writer.PropertyName("id");
				writer.Write(budgetItem.m_ID);
				writer.PropertyName("color");
				writer.Write(budgetItem.m_Color);
				writer.PropertyName("icon");
				writer.Write(budgetItem.m_Icon);
				writer.PropertyName("sources");

				var subItems = budgetItem.m_Sources.Length;

				//if (budgetItem.m_ID == "Exports")
				//{
				//	subItems++;
				//}

				writer.ArrayBegin(subItems);
				var sources = budgetItem.m_Sources;
				foreach (var incomeSource in sources)
				{
					writer.TypeBegin("Game.UI.InGame.BudgetSource");
					writer.PropertyName("id");
					writer.Write(Enum.GetName(typeof(IncomeSource), incomeSource));
					writer.PropertyName("index");
					writer.Write((int)incomeSource);
					writer.TypeEnd();
				}

				//if (budgetItem.m_ID == "Exports")
				//{
				//	writer.TypeBegin("Game.UI.InGame.BudgetSource");
				//	writer.PropertyName("id");
				//	writer.Write("ExportedGoods");
				//	writer.PropertyName("index");
				//	writer.Write(14);
				//	writer.TypeEnd();
				//}

				writer.ArrayEnd();
				writer.TypeEnd();
			}

			writer.ArrayEnd();
		}

		private void BindIncomeValues(IJsonWriter writer)
		{
			writer.ArrayBegin((uint)IncomeSource.Count);
			for (var i = 0; i < (int)IncomeSource.Count; i++)
			{
				writer.Write(EconomyUtility.GetIncome((IncomeSource)i, m_CityServiceBudgetSystem.GetIncome((IncomeSource)i)));
			}

			//var m_City = World.GetOrCreateSystemManaged<CitySystem>().City;
			//var money = 0;

			//if (EntityManager.TryGetBuffer<BufferedMoneyResource>(m_City, true, out var moneyBuffer))
			//{
			//	for (var i = 0; i < moneyBuffer.Length; i++)
			//	{
			//		var bufferedMoney = moneyBuffer[i];
			//		if (bufferedMoney.m_Value > 0)
			//		{
			//			money += EconomyUtility.GetIncome(IncomeSource.Count, bufferedMoney.m_Value);
			//		}
			//	}
			//}

			//writer.Write(money);
			writer.ArrayEnd();
		}

		private void BindExpenseItems(IJsonWriter writer)
		{
			var config = GetConfig();
			writer.ArrayBegin(config.m_ExpenseItems.Length);
			var expenseItems = config.m_ExpenseItems;
			foreach (var budgetItem in expenseItems)
			{
				writer.TypeBegin("Game.UI.InGame.BudgetItem");
				writer.PropertyName("id");
				writer.Write(budgetItem.m_ID);
				writer.PropertyName("color");
				writer.Write(budgetItem.m_Color);
				writer.PropertyName("icon");
				writer.Write(budgetItem.m_Icon);
				writer.PropertyName("sources");
				var subItems = budgetItem.m_Sources.Length;

				if (budgetItem.m_ID is /*"Imports" or*/ "Upkeeps")
				{
					subItems++;
				}

				writer.ArrayBegin(subItems);
				var sources = budgetItem.m_Sources;
				foreach (var expenseSource in sources)
				{
					writer.TypeBegin("Game.UI.InGame.BudgetSource");
					writer.PropertyName("id");
					writer.Write(Enum.GetName(typeof(ExpenseSource), expenseSource));
					writer.PropertyName("index");
					writer.Write((int)expenseSource);
					writer.TypeEnd();
				}

				if (budgetItem.m_ID == "Upkeeps")
				{
					writer.TypeBegin("Game.UI.InGame.BudgetSource");
					writer.PropertyName("id");
					writer.Write("BuildingDemolitions");
					writer.PropertyName("index");
					writer.Write((int)IncomeSource.Count);
					writer.TypeEnd();
				}

				//if (budgetItem.m_ID == "Imports")
				//{
				//	writer.TypeBegin("Game.UI.InGame.BudgetSource");
				//	writer.PropertyName("id");
				//	writer.Write("ImportedGoods");
				//	writer.PropertyName("index");
				//	writer.Write(10);
				//	writer.TypeEnd();
				//}

				writer.ArrayEnd();
				writer.TypeEnd();
			}

			writer.ArrayEnd();
		}

		private void BindExpenseValues(IJsonWriter writer)
		{
			writer.ArrayBegin(1u + (uint)ExpenseSource.Count);
			for (var i = 0; i < (int)ExpenseSource.Count; i++)
			{
				writer.Write(-EconomyUtility.GetExpense((ExpenseSource)i, m_CityServiceBudgetSystem.GetExpense((ExpenseSource)i)));
			}

			var m_City = World.GetOrCreateSystemManaged<CitySystem>().City;

			var demoCost = 0;
			if (EntityManager.TryGetBuffer<PendingDemolitionCosts>(m_City, true, out var demoCostsBuffer))
			{
				for (var i = 0; i < demoCostsBuffer.Length; i++)
				{
					var demoCosts = demoCostsBuffer[i];
					demoCost -= demoCosts.m_Value;
				}
			}

			writer.Write(demoCost);

			//var money = 0;
			//if (EntityManager.TryGetBuffer<BufferedMoneyResource>(m_City, true, out var moneyBuffer))
			//{
			//	for (var i = 0; i < moneyBuffer.Length; i++)
			//	{
			//		var bufferedMoney = moneyBuffer[i];
			//		if (bufferedMoney.m_Value < 0)
			//		{
			//			money += EconomyUtility.GetExpense(ExpenseSource.Count, bufferedMoney.m_Value);
			//		}
			//	}
			//}

			//writer.Write(money);

			writer.ArrayEnd();
		}

		private UIEconomyConfigurationPrefab GetConfig()
		{
			return m_PrefabSystem.GetSingletonPrefab<UIEconomyConfigurationPrefab>(m_ConfigQuery);
		}
	}
}
