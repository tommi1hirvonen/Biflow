using EtlManager.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManager.DataAccess;

public interface ITokenService
{
    public Task<string> GetTokenAsync(AppRegistration appRegistration, string resourceUrl);

    public void Clear();
}
