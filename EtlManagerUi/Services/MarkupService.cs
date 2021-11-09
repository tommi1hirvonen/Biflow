using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace EtlManagerUi;

public class MarkupService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MarkupService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public MarkupString MarkupFromFile(string relativeResourcePath)
    {
        var path = Path.Combine(_webHostEnvironment.WebRootPath, relativeResourcePath);
        var text = File.ReadAllText(path);
        return (MarkupString)text;
    }
}
