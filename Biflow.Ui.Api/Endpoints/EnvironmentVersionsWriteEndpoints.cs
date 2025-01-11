using Biflow.Ui.Core.Projection;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class EnvironmentVersionsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.EnvironmentVersionsWrite]);
        
        var group = app.MapGroup("/environmentversions")
            .WithTags(Scopes.EnvironmentVersionsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("",
            async ([FromBody] EnvironmentVersionDto model, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new CreateVersionCommand(model.Description);
                var version = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(version);
            })
            .Produces<VersionProjection>()
            .WithDescription("Create a new environment version from the current environment state.")
            .WithName("CreateEnvironmentVersion");
    }
}