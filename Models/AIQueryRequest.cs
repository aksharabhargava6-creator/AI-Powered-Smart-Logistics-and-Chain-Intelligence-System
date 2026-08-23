namespace SmartLogistics.API.Models;

public class AIQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new List<string>();
    public double Confidence { get; set; }
    public string ContextUsed { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
}