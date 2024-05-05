using Colossal.Serialization.Entities;

using Unity.Entities;

namespace HardMode.Domain
{
	public struct PendingDemolitionCosts : IBufferElementData, ISerializable
	{
		public int m_Value;
		public int m_SimulationTick;

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(m_Value);
			writer.Write(m_SimulationTick);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out m_Value);
			reader.Read(out m_SimulationTick);
		}
	}
}
