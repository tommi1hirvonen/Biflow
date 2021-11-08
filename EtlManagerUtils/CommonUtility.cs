using System.IO;
using System.IO.Pipes;

namespace EtlManagerUtils;

public static class CommonUtility
{
    public static byte[] ReadMessage(PipeStream pipe)
    {
        byte[] buffer = new byte[1024];
        using var ms = new MemoryStream();
        do
        {
            var readBytes = pipe.Read(buffer, 0, buffer.Length);
            ms.Write(buffer, 0, readBytes);
        }
        while (!pipe.IsMessageComplete);

        return ms.ToArray();
    }
}
