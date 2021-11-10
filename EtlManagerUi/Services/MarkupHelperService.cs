using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace EtlManagerUi;

public class MarkupHelperService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MarkupHelperService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public MarkupString FromFile(string relativeResourcePath)
    {
        var path = Path.Combine(_webHostEnvironment.WebRootPath, relativeResourcePath);
        var text = File.ReadAllText(path);
        return (MarkupString)text;
    }
}
