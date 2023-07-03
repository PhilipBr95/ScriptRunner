using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IHistoryService
    {
        Task<IList<Activity<T>>> GetActivitiesAsync<T>();
        Task LogActivityAsync<T>(Activity<T> activity);
    }
}
