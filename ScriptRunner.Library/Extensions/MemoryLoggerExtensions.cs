using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ScriptRunner.Library.Services;

namespace ScriptRunner.Library.Extensions
{
    public static class MemoryLoggerExtensions
    {
        public static ILoggingBuilder AddMemoryLogger(this ILoggingBuilder builder)
        {
            var memoryLoggerProvider = new MemoryLoggerProvider();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(memoryLoggerProvider));
            builder.Services.TryAddSingleton<IMemoryLoggerProvider>(memoryLoggerProvider);

            return builder;
        }
    }
}
