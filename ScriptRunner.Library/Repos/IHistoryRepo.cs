using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Repos
{
    public interface IHistoryRepo
    {
        Task SaveActiviesAsync<T>(IList<Activity<T>> activities);
        Task<IList<Activity<T>>> LoadActivitiesAsync<T>();
    }
}