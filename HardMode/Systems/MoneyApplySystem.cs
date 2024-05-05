using Colossal.Serialization.Entities;

using Game;
using Game.City;
using Game.Simulation;

using HardMode.Domain;
using HardMode.Utility;

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace HardMode.Systems
{
	public partial class MoneyApplySystem : GameSystemBase
	{
		public const int kUpdatesPerDay = 1024;

		private CitySystem m_CitySystem;
		private CityServiceBudgetSystem m_CityServiceBudgetSystem;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / kUpdatesPerDay;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_CityServiceBudgetSystem = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);

			World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BudgetApplySystem>().Enabled = false;
		}

		protected override void OnUpdate()
		{
			var budgetApplyJob = new MoneyApplyJob
			{
				m_PlayerMoneys = SystemAPI.GetComponentLookup<PlayerMoney>(),
				m_DemolitionCosts = SystemAPI.GetBufferLookup<PendingDemolitionCosts>(),
				m_BufferedMoney = SystemAPI.GetBufferLookup<BufferedMoneyResource>(),
				m_City = m_CitySystem.City,
				m_Expenses = m_CityServiceBudgetSystem.GetExpenseArray(out var deps),
				m_Income = m_CityServiceBudgetSystem.GetIncomeArray(out var deps2)
			};

			Dependency = IJobExtensions.Schedule(budgetApplyJob, JobHandle.CombineDependencies(Dependency, deps, deps2));

			//m_CityServiceBudgetSystem.AddArrayReader(Dependency);
		}

		private struct MoneyApplyJob : IJob
		{
			public NativeArray<int> m_Income;
			public NativeArray<int> m_Expenses;

			public ComponentLookup<PlayerMoney> m_PlayerMoneys;
			public BufferLookup<PendingDemolitionCosts> m_DemolitionCosts;
			public BufferLookup<BufferedMoneyResource> m_BufferedMoney;

			public Entity m_City;

			public void Execute()
			{
				var num = 0;
				for (var i = 0; i < 9; i++)
				{
					num -= EconomyUtility.GetExpense((ExpenseSource)i, m_Expenses);
				}

				for (var j = 0; j < 14; j++)
				{
					num += EconomyUtility.GetIncome((IncomeSource)j, m_Income);
				}

				if (m_BufferedMoney.TryGetBuffer(m_City, out var moneyBuffer))
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

						money.m_SimulationTick++;

						if (money.m_SimulationTick == kUpdatesPerDay)
						{
							moneyBuffer.RemoveAt(i);
							i--;
						}
						else
						{
							moneyBuffer[i] = money;
						}
					}
				}

				if (m_DemolitionCosts.TryGetBuffer(m_City, out var demoCostBuffer))
				{
					for (var i = 0; i < demoCostBuffer.Length; i++)
					{
						var demoCost = demoCostBuffer[i];

						num -= demoCost.m_Value;

						demoCost.m_SimulationTick++;

						if (demoCost.m_SimulationTick == kUpdatesPerDay)
						{
							demoCostBuffer.RemoveAt(i);
							i--;
						}
						else
						{
							demoCostBuffer[i] = demoCost;
						}
					}
				}

				var playerMoney = m_PlayerMoneys[m_City];

				playerMoney.Add(num / kUpdatesPerDay);

				m_PlayerMoneys[m_City] = playerMoney;
			}
		}
	}
}
