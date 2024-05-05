using Game.Common;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;

using Unity.Entities;

namespace HardMode.Systems
{
	public partial class BulldozeCostTooltipSystem : TooltipSystemBase
	{
		private ToolSystem m_ToolSystem;
		private BulldozeToolSystem m_BulldozeTool;
		private BulldozeCostSystem m_BulldozeCostSystem;
		private IntTooltip m_Tooltip;

		private EntityQuery m_TempQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			m_BulldozeTool = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
			m_BulldozeCostSystem = World.GetOrCreateSystemManaged<BulldozeCostSystem>();
			m_TempQuery = SystemAPI.QueryBuilder().WithAll<Temp>().WithNone<Deleted>().Build();

			RequireForUpdate(m_TempQuery);

			m_Tooltip = new IntTooltip
			{
				icon = "Media/Game/Icons/Money.svg",
				path = "bulldozeCostTool",
				unit = "money",
				color = TooltipColor.Warning,
				label = LocalizedString.Id("HardMode.BULLDOZECOST_LABEL")
			};
		}

		protected override void OnUpdate()
		{
			if (m_ToolSystem.activeTool != m_BulldozeTool)
			{
				return;
			}

			if (m_BulldozeCostSystem.TotalCost > 0)
			{
				m_Tooltip.value = m_BulldozeCostSystem.TotalCost;
				m_Tooltip.color = TooltipColor.Info;

				AddMouseTooltip(m_Tooltip);
			}
		}
	}
}
