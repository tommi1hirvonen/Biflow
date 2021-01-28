using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SqlStep : StepBase
    {
        public string SqlStatement { get; init; }
        public string ConnectionString { get; init; }

        public SqlStep(ConfigurationBase configuration, string stepId, string sqlStatement, string connectionString)
            : base(configuration, stepId)
        {
            SqlStatement = sqlStatement;
            ConnectionString = connectionString;
        }
    }
}
