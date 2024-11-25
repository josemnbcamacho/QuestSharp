using QuestSharp.Models;

namespace QuestSharp;

public class GoalBuilder
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _opener = null;
    private readonly List<GoalField> _fields = new();

    public GoalBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public GoalBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public GoalBuilder WithOpener(string opener)
    {
        _opener = opener;
        return this;
    }

    public GoalBuilder AddField(string name, string description, Func<object, bool>? validator = null)
    {
        _fields.Add(new GoalField(name, description, validator));
        return this;
    }

    public Goal Build()
    {
        if (string.IsNullOrEmpty(_name))
            throw new InvalidOperationException("Goal name is required");
        
        if (string.IsNullOrEmpty(_description))
            throw new InvalidOperationException("Goal description is required");

        return new Goal(_name, _description, _opener, _fields);
    }
} 