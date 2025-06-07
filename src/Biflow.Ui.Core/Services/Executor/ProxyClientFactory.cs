namespace Biflow.Ui.Core;

public class ProxyClientFactory(IHttpClientFactory httpClientFactory)
{
    public ProxyClient Create(Proxy proxy) => new(httpClientFactory, proxy);
}