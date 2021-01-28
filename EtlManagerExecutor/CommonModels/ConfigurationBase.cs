using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ConfigurationBase
    {
        public string ConnectionString { get; init; }
        public string ExecutionId { get; init; }
        public string EncryptionKey { get; init; }
        public string Username { get; set; }

        public ConfigurationBase(string connectionString, string executionId, string encryptionKey, string username)
        {
            ConnectionString = connectionString;
            ExecutionId = executionId;
            EncryptionKey = encryptionKey;
            Username = username;
        }
    }
}
