using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public class StepDuplicator : IDisposable
{
    private readonly BiflowContext _context;

    internal StepDuplicator(BiflowContext context, Step step)
    {
        _context = context;
        Step = step;
    }

    public Step Step { get; }

    public async Task<Step> SaveStepAsync()
    {
        _context.Steps.Add(Step);
        await _context.SaveChangesAsync();
        return Step;
    }

    public void Dispose() => _context.Dispose();
}
