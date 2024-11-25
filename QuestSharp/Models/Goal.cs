namespace QuestSharp.Models;

public class Goal
{
    public string Name { get; }
    public string Description { get; }
    public string Opener { get; }
    public List<GoalField> Fields { get; }
    public List<GoalConnection> Connections { get; }
    public Goal(string name, string description, string opener, List<GoalField> fields)
    {
        Name = name;
        Description = description;
        Opener = opener;
        Fields = fields;
        Connections = new List<GoalConnection>();
    }

    public void Connect(Goal goal, string userGoal, bool handOver = false, bool keepMessages = false)
    {
        Connections.Add(new GoalConnection
        {
            TargetGoal = goal,
            UserIntent = userGoal,
            HandOver = handOver,
            KeepMessages = keepMessages
        });
    }
}

public class GoalConnection
{
    public Goal TargetGoal { get; set; } = null!;
    public string UserIntent { get; set; } = string.Empty;
    public bool HandOver { get; set; }
    public bool KeepMessages { get; set; }
} 