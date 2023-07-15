using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Repos
{
    public interface IHistoryRepo
    {
        Task SaveActivityAsync<T>(Activity<T> activity);
        Task<IList<Activity<T>>> LoadActivitiesAsync<T>();
    }
}