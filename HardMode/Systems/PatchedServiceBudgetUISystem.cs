using Colossal.Entities;
using Colossal.UI.Binding;

using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;

using HardMode.Utility;

using System.Reflection;

using Unity.Entities;

namespace HardMode.Systems
{
	internal partial class PatchedServiceBudgetUISystem : SystemBase
	{
		private PrefabSystem m_PrefabSystem;
		private CitySystem m_CitySystem;
		private CityServiceBudgetSystem m_CityServiceBudgetSystem;
		private ServiceFeeSystem m_ServiceFeeSystem;
		private MethodInfo WriteServiceFees;

		protected override void OnCreate()
		{
			m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_CityServiceBudgetSystem = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
			m_ServiceFeeSystem = World.GetOrCreateSystemManaged<ServiceFeeSystem>();

			WriteServiceFees = typeof(Game.UI.InGame.ServiceBudgetUISystem).GetMethod("WriteServiceFees", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		}

		protected override void OnUpdate()
		{
		}

		internal int GetTotalBudget(Entity service, DynamicBuffer<ServiceFee> fees)
		{
			m_CityServiceBudgetSystem.GetEstimatedServiceBudget(service, out var upkeep);
			var num = -EconomyUtility.GetExpense(ExpenseSource.ServiceUpkeep, upkeep);
			if (EntityManager.TryGetComponent<ServiceData>(service, out var component) && EntityManager.TryGetBuffer(service, isReadOnly: true, out DynamicBuffer<CollectedCityServiceFeeData> buffer))
			{
				foreach (var item in buffer)
				{
					var playerResource = (PlayerResource)item.m_PlayerResource;
					var num2 = ServiceFeeSystem.TryGetFee(playerResource, fees, out var fee) ? m_ServiceFeeSystem.GetServiceFeeIncomeEstimate(playerResource, fee) : m_ServiceFeeSystem.GetServiceFees(playerResource).x;
					var serviceFees = m_ServiceFeeSystem.GetServiceFees(playerResource);

					switch (component.m_Service)
					{
						case CityService.Electricity:
							num += EconomyUtility.GetIncome(IncomeSource.FeeElectricity, num2);
							num += EconomyUtility.GetIncome(IncomeSource.ExportElectricity, serviceFees.y);
							num -= EconomyUtility.GetExpense(ExpenseSource.ImportElectricity, serviceFees.z);
							break;
						case CityService.GarbageManagement:
							num += EconomyUtility.GetIncome(IncomeSource.FeeGarbage, num2);
							num += EconomyUtility.GetIncome(IncomeSource.FeeGarbage, serviceFees.y);
							num -= EconomyUtility.GetExpense(ExpenseSource.ServiceUpkeep, serviceFees.z);
							break;
						case CityService.HealthcareAndDeathcare:
							num += EconomyUtility.GetIncome(IncomeSource.FeeHealthcare, num2);
							num += EconomyUtility.GetIncome(IncomeSource.FeeHealthcare, serviceFees.y);
							num -= EconomyUtility.GetExpense(ExpenseSource.ServiceUpkeep, serviceFees.z);
							break;
						case CityService.WaterAndSewage:
							num += EconomyUtility.GetIncome(IncomeSource.FeeWater, num2);
							num += EconomyUtility.GetIncome(IncomeSource.ExportWater, serviceFees.y);
							num -= EconomyUtility.GetExpense(ExpenseSource.ImportWater, serviceFees.z);
							break;
						default:
							num += EconomyUtility.GetIncome(IncomeSource.Count, num2);
							num += EconomyUtility.GetIncome(IncomeSource.Count, serviceFees.y);
							num -= EconomyUtility.GetExpense(ExpenseSource.ServiceUpkeep, serviceFees.z);
							break;
					}
				}
			}

			return num;
		}

		internal void WriteServiceDetails(IJsonWriter writer, Entity serviceEntity, Game.UI.InGame.ServiceBudgetUISystem instance)
		{
			if (EntityManager.TryGetComponent<ServiceData>(serviceEntity, out var component) && EntityManager.TryGetComponent<PrefabData>(serviceEntity, out var component2))
			{
				var prefab = m_PrefabSystem.GetPrefab<ServicePrefab>(component2);
				m_CityServiceBudgetSystem.GetEstimatedServiceBudget(serviceEntity, out var upkeep);
				writer.TypeBegin("serviceBudget.ServiceDetails");
				writer.PropertyName("entity");
				writer.Write(serviceEntity);
				writer.PropertyName("name");
				writer.Write(prefab.name);
				writer.PropertyName("icon");
				writer.Write(ImageSystem.GetIcon(prefab));
				writer.PropertyName("locked");
				writer.Write(EntityManager.HasEnabledComponent<Locked>(serviceEntity));
				writer.PropertyName("budgetAdjustable");
				writer.Write(component.m_BudgetAdjustable);
				var serviceBudget = m_CityServiceBudgetSystem.GetServiceBudget(serviceEntity);
				writer.PropertyName("budgetPercentage");
				writer.Write(serviceBudget);
				writer.PropertyName("efficiency");
				writer.Write(m_CityServiceBudgetSystem.GetServiceEfficiency(serviceEntity, serviceBudget));
				writer.PropertyName("upkeep");
				writer.Write(-EconomyUtility.GetExpense(ExpenseSource.ServiceUpkeep, upkeep));
				writer.PropertyName("fees");
				WriteServiceFees.Invoke(instance, new object[] { writer, serviceEntity });
				writer.TypeEnd();
			}
			else
			{
				writer.WriteNull();
			}
		}
	}
}
