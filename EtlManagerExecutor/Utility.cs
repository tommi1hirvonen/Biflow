using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace EtlManagerExecutor
{
    public abstract class ExecutionResult
    {
        public class Success : ExecutionResult { }
        public class Failure : ExecutionResult
        {
            public string ErrorMessage { get; } = string.Empty;
            public Failure(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }
        }
    }

    public static class Utility
    {
        

        public static void OpenIfClosed(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open && sqlConnection.State != ConnectionState.Connecting)
            {
                sqlConnection.Open();
            }
        }
    }
}
