using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace EtlManagerExecutor
{
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
