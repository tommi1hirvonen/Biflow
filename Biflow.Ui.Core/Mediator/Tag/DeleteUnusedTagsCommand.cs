using MediatR;

namespace Biflow.Ui.Core;

public record DeleteUnusedTagsCommand : IRequest<DeleteUnusedTagsResponse>;