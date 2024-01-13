namespace Biflow.DataAccess;

public class StepsDuplicator : IDisposable
{
    private readonly AppDbContext _context;

    internal StepsDuplicator(AppDbContext context, List<Step> steps)
    {
        _context = context;
        Steps = steps;
    }

    public IList<Step> Steps { get; }

    public async Task<IEnumerable<Step>> SaveStepsAsync()
    {
        _context.Steps.AddRange(Steps);
        await _context.SaveChangesAsync();
        return Steps;
    }

    public void Dispose() => _context.Dispose();
}
