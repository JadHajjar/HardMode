using Colossal.Serialization.Entities;

using Unity.Entities;

namespace HardMode.Domain
{
	public struct DemolitionTimer : IComponentData, IQueryTypeParameter, ISerializable
	{
		public int mTimer;
		private const int CurrentVersion = 1;

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out int version);
			reader.Read(out mTimer);
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(CurrentVersion);
			writer.Write(mTimer);
		}
	}
}
