using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class SqlConnectionInfo : ConnectionInfoBase
    {

        public SqlConnectionInfo(string connectionName, string connectionString)
            : base(ConnectionType.Sql, connectionName, connectionString) { }

        [Display(Name = "Execute packages as login")]
        public string? ExecutePackagesAsLogin
        {
            get => _executePackagesAsLogin;
            set => _executePackagesAsLogin = string.IsNullOrEmpty(value) ? null : value;
        }

        private string? _executePackagesAsLogin;

    }
}
