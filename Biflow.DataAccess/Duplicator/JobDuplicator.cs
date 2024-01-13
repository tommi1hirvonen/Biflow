using Biflow.Core.Entities;

namespace Biflow.DataAccess;

public class JobDuplicator : IDisposable
{
    private readonly AppDbContext _context;

    internal JobDuplicator(AppDbContext context, Job job)
    {
        _context = context;
        Job = job;
    }

    public Job Job { get; set; }

    public async Task<Job> SaveJobAsync()
    {
        _context.Jobs.Add(Job);
        await _context.SaveChangesAsync();
        return Job;
    }

    public void Dispose() => _context.Dispose();
}
