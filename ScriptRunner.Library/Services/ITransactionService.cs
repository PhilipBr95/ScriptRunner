﻿using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface ITransactionService
    {
        Task LogActivityAsync<T>(Activity<T> activity);
    }
}
