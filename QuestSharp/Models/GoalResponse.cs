namespace QuestSharp.Models;

public enum GoalResponseType
{
    Continue,
    Complete,
    Invalid,
    Error
}

public class GoalResponse
{
    public string Message { get; set; } = string.Empty;
    public GoalResponseType Type { get; set; }
    public Dictionary<string, object>? Data { get; set; }
} 