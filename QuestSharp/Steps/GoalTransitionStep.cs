using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using QuestSharp.Models;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0003
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0021
#pragma warning disable SKEXP0022
#pragma warning disable SKEXP0023
#pragma warning disable SKEXP0024
#pragma warning disable SKEXP0025
#pragma warning disable SKEXP0026
#pragma warning disable SKEXP0027

namespace QuestSharp.Steps;

[KernelProcessStepMetadata("GoalTransition.V1")]
public class GoalTransitionStep : KernelProcessStep
{
    public static class Functions
    {
        public const string HandleTransition = nameof(HandleTransition);
    }

    public static class OutputEvents
    {
        public const string TransitionAccepted = nameof(TransitionAccepted);
    }

    [KernelFunction(Functions.HandleTransition)]
    public async Task HandleTransitionAsync(KernelProcessStepContext context, Kernel kernel, GoalTransitionData data)
    {
        var currentGoal = kernel.Data["CurrentGoal"] as Goal;
        var connection = currentGoal?.Connections.FirstOrDefault(c => c.TargetGoal == data.Goal);
        
        if (connection != null && !connection.KeepMessages)
        {
            kernel.Data["ConversationHistory"] = new List<(string Role, string Content)>();
        }
        
        kernel.Data["CurrentGoal"] = data.Goal;
        
        await context.EmitEventAsync(new()
        {
            Id = OutputEvents.TransitionAccepted,
            Data = data.UserInput
        });
    }
}

public sealed class GoalTransitionData
{
    public Goal Goal { get; set; }
    public string UserInput { get; set; }
}