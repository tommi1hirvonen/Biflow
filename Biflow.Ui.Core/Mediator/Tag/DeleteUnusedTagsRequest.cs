using MediatR;

namespace Biflow.Ui.Core;

public record DeleteUnusedTagsRequest : IRequest<DeleteUnusedTagsResponse>;