using ScriptRunner.Library.Models;

namespace ScriptRunner.UI.Services
{
    public interface IHistoryRepo
    {
        Task SaveActiviesAsync<T>(IList<Activity<T>> activities);

        Task<IList<Activity<T>>> LoadActivitiesAsync<T>();
    }
}