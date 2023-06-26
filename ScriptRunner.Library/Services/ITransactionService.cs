using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptRunner.Library.Models;

namespace ScriptRunner.Library.Services
{
    public interface ITransactionService
    {
        void LogActivity<T>(Activity<T> activity);
    }
}
