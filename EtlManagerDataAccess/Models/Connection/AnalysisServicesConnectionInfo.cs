using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class AnalysisServicesConnectionInfo : ConnectionInfoBase
    {
        public AnalysisServicesConnectionInfo(string connectionName, string connectionString)
            : base(ConnectionType.AnalysisServices, connectionName, connectionString) { }
    }
}
