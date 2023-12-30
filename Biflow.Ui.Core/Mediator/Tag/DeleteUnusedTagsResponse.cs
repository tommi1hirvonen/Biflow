using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public record DeleteUnusedTagsResponse(IEnumerable<Tag> DeletedTags);