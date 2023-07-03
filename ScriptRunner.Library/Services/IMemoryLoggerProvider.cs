using System.Buffers;

namespace ScriptRunner.Library.Services
{
    public interface IMemoryLoggerProvider
    {
        IReadOnlyList<MemoryLog> MemoryLogs { get; }
    }
}