using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using QuestSharp.Models;
using QuestSharp.Steps;
using QuestSharp.Processes;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0003

namespace QuestSharp;

public class QuestService : IQuestService
{
    private readonly Kernel _kernel;
    private Goal _currentGoal;
    private KernelProcess? _process;
    private KernelProcessContext? _processContext;

    public QuestService(Kernel kernel, Goal initialGoal)
    {
        _kernel = kernel;
        _currentGoal = initialGoal;
        
        // Initialize conversation history in kernel data
        _kernel.Data["ConversationHistory"] = new List<(string Role, string Content)>();
    }

    public async Task ProcessInputAsync(string userInput)
    {
        _process ??= GoalProcess.CreateProcess(_currentGoal).Build();
        
        _kernel.Data["CurrentGoal"] = _currentGoal;

        _processContext = await _process.StartAsync(_kernel, new KernelProcessEvent()
        {
            Id = GoalProcess.ProcessEvents.StartGoal,
            Data = userInput
        });
    }
} 