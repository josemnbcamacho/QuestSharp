namespace QuestSharp.Models;

public class GoalField
{
    public string Name { get; }
    public string Description { get; }
    public Func<object, bool>? Validator { get; }

    public GoalField(string name, string description, Func<object, bool>? validator = null)
    {
        Name = name;
        Description = description;
        Validator = validator;
    }
} 