namespace ScriptRunner.Library.Services
{
    public interface IPowerShellExecutorResolver
    {
        IPowerShellExecutor Resolve(Models.Options? options);
    }
}
