using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using QuestSharp.Models;

#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0003

namespace QuestSharp.Steps;

[KernelProcessStepMetadata("RenderResponse.V1")]
public class RenderResponseStep : KernelProcessStep<RenderResponseState>
{
    public static class Functions
    {
        public const string RenderResponse = nameof(RenderResponse);
    }

    public static class OutputEvents
    {
        public const string ResponseReady = nameof(ResponseReady);
    }

    private RenderResponseState? _state;

    public override ValueTask ActivateAsync(KernelProcessStepState<RenderResponseState> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    [KernelFunction(Functions.RenderResponse)]
    public async Task RenderResponseAsync(KernelProcessStepContext context, Kernel kernel, object? data = null)
    {
        kernel.Data.TryGetValue("CurrentGoal", out var goal);
        
        if (goal is not Goal currentGoal)
        {
            throw new InvalidOperationException("Current goal not found in kernel data.");
        }
        
        var response = data switch
        {
            ValidationFailedData validationFailed => new GoalResponse
            {
                Message = validationFailed.ErrorMessage,
                Type = GoalResponseType.Invalid
            },
            
            Dictionary<string, object> completionData => new GoalResponse
            {
                Message = $"The order is complete with the following details: ",
                Type = GoalResponseType.Complete,
                Data = completionData
            },
            
            string promptMessage => new GoalResponse
            {
                Message = promptMessage,
                Type = GoalResponseType.Continue
            },
            
            _ => new GoalResponse
            {
                Message = "Please provide the requested information.",
                Type = GoalResponseType.Continue
            }
        };

        await context.EmitEventAsync(new()
        {
            Id = OutputEvents.ResponseReady,
            Data = response,
            Visibility = KernelProcessEventVisibility.Public
        });
    }
}

public sealed class RenderResponseState
{
}
