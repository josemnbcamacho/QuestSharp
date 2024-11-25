using QuestSharp.Models;

namespace QuestSharp;

public interface IQuestService
{
    Task ProcessInputAsync(string userInput);
} 