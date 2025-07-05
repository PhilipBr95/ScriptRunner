using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Repos
{
    public interface IHistoryRepo
    {
        Task SaveActivityAsync<T>(ActivityWithData<T> activity);
        Task<IList<ActivityWithData<T>>> LoadActivitiesAsync<T>();
    }
}