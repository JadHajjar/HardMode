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
using System.Collections.Generic;

using Unity.Entities;

namespace HardMode.Systems
{
	public partial class CustomBudgetUISystem : UISystemBase
	{
		private Dictionary<string, Func<bool>> m_BudgetsActivations;
		private PrefabSystem m_PrefabSystem;
		private BudgetUISystem m_BudgetUISystem;
		private CityServiceBudgetSystem m_CityServiceBudgetSystem;

		private EntityQuery m_ConfigQuery;
		private GameModeGovernmentSubsidiesSystem m_GovernmentSubsidiesSystem;
		private CityConfigurationSystem m_CityConfigurationSystem;
		private MapTilePurchaseSystem m_MapTilePurchaseSystem;
		private CitySystem m_CitySystem;
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
			m_GovernmentSubsidiesSystem = World.GetOrCreateSystemManaged<GameModeGovernmentSubsidiesSystem>();
			m_CityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
			m_MapTilePurchaseSystem = World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_BudgetsActivations = new Dictionary<string, Func<bool>>
			{
				{
					"Government",
					() => m_GovernmentSubsidiesSystem.Enabled
				},
				{
					"Loan Interest",
					() => !m_CityConfigurationSystem.unlimitedMoney
				},
				{
					"Tile Upkeep",
					delegate
					{
						if (m_CityConfigurationSystem.unlockMapTiles)
						{
							return false;
						}

						var singleton = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>()).GetSingleton<EconomyParameterData>();
						var num = 0f;
						for (var i = 0; i <= 100; i += 10)
						{
							num = singleton.m_MapTileUpkeepCostMultiplier.Evaluate(i);
							if (num > 0f)
							{
								return true;
							}
						}

						return num != 0f;
					}
				}
			};

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
				writer.PropertyName("active");
				writer.Write(!m_BudgetsActivations.ContainsKey(budgetItem.m_ID) || m_BudgetsActivations[budgetItem.m_ID]());
				writer.PropertyName("sources");
				writer.ArrayBegin(budgetItem.m_Sources.Length);
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

				writer.ArrayEnd();
				writer.TypeEnd();
			}

			writer.ArrayEnd();
		}

		private void BindIncomeValues(IJsonWriter writer)
		{
			writer.ArrayBegin(14u);
			for (var i = 0; i < 14; i++)
			{
				writer.Write(EconomyUtility.GetIncome((IncomeSource)i, m_CityServiceBudgetSystem.GetIncome((IncomeSource)i)));
			}

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
				writer.PropertyName("active");
				writer.Write(!m_BudgetsActivations.ContainsKey(budgetItem.m_ID) || m_BudgetsActivations[budgetItem.m_ID]());
				writer.PropertyName("sources");
				writer.ArrayBegin((budgetItem.m_ID == "Upkeeps" ? 1 : 0) + budgetItem.m_Sources.Length);
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

				writer.ArrayEnd();
				writer.TypeEnd();
			}

			writer.ArrayEnd();
		}

		private void BindExpenseValues(IJsonWriter writer)
		{
			writer.ArrayBegin(16u);
			for (var i = 0; i < 15; i++)
			{
				writer.Write(-EconomyUtility.GetExpense((ExpenseSource)i, m_CityServiceBudgetSystem.GetExpense((ExpenseSource)i)));
			}

			var m_City = m_CitySystem.City;
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

			writer.ArrayEnd();
		}

		private UIEconomyConfigurationPrefab GetConfig()
		{
			return m_PrefabSystem.GetSingletonPrefab<UIEconomyConfigurationPrefab>(m_ConfigQuery);
		}
	}
}
