using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface IHistoryService
    {
        Task<IList<ActivityWithData<T>>> GetActivitiesAsync<T>();
        Task LogActivityAsync<T>(ActivityWithData<T> activity);
    }
}
