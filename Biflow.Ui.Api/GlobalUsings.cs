global using Biflow.Core.Constants;
global using Biflow.Core.Entities;
global using Biflow.DataAccess;
global using Biflow.Ui.Api.Exceptions;
global using Biflow.Ui.Api.Models;
global using Biflow.Ui.Api.Models.Step;
global using Biflow.Ui.Api.Services;
global using Biflow.Ui.Core;
global using JetBrains.Annotations;
global using Microsoft.EntityFrameworkCore;
global using VersionRevertJobDictionary =
    System.Collections.Concurrent.ConcurrentDictionary<System.Guid, Biflow.Ui.Api.Models.VersionRevertJobState>;