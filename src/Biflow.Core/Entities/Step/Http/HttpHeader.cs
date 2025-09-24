namespace Biflow.Core.Entities;

public record HttpHeader
{
    public HttpHeader(string key, string value) => (Key, Value) = (key, value);
    
    public HttpHeader() { }
    
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}