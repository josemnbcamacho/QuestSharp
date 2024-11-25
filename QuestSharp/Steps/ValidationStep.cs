using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using QuestSharp.Models;
#pragma warning disable SKEXP0080
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

[KernelProcessStepMetadata("Validation.V1")]
public class ValidationStep : KernelProcessStep<ValidationState>
{
    public static class Functions
    {
        public const string ValidateJson = nameof(ValidateJson);
    }

    public static class OutputEvents
    {
        public const string ValidationPassed = nameof(ValidationPassed);
        public const string ValidationFailed = nameof(ValidationFailed);
    }

    private ValidationState? _state;

    public override ValueTask ActivateAsync(KernelProcessStepState<ValidationState> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    [KernelFunction(Functions.ValidateJson)]
    public async Task ValidateJsonAsync(KernelProcessStepContext context, Kernel kernel, Dictionary<string, object> data)
    {
        kernel.Data.TryGetValue("CurrentGoal", out var goal);
        
        if (goal is not Goal currentGoal)
        {
            throw new InvalidOperationException("Current goal not found in kernel data.");
        }
        
        kernel.Data.TryGetValue("ConversationHistory", out var history);

        _state.Fields = currentGoal.Fields;
        
        var errors = new List<string>();

        // Validate all required fields are present and valid
        foreach (var field in _state!.Fields)
        {
            if (!data.TryGetValue(field.Name, out var value))
            {
                errors.Add($"Missing value for {field.Description}");
                continue;
            }

            if (field.Validator != null && !field.Validator(value))
            {
                errors.Add($"Invalid value for {field.Description}");
            }
        }

        if (errors.Any())
        {
            // Create prompt for error message generation
            var promptConfig = new PromptTemplateConfig
            {
                Template = @"Your role is to continue the conversation below as the Assistant.
Unfortunately you had trouble processing the user's request because of the following problems:
{{$errors}}

Continue the conversation naturally, and explain the problems.
Do not be creative. Do not make suggestions as to how to fix the problems.

Conversation so far:
{{$messages}}
"
            };

            var args = new KernelArguments
            {
                { "errors", string.Join("\n", errors) },
                { "messages", string.Join("\n", history) }
            };
            
            var evaluationFunction = kernel.CreateFunctionFromPrompt(promptConfig.Template);
            var result = await evaluationFunction.InvokeAsync(kernel, args);

            await context.EmitEventAsync(new()
            {
                Id = OutputEvents.ValidationFailed,
                Data = new ValidationFailedData
                {
                    ErrorMessage = result.ToString()
                }
            });
            return;
        }

        await context.EmitEventAsync(new()
        {
            Id = OutputEvents.ValidationPassed,
            Data = data,
            Visibility = KernelProcessEventVisibility.Public
        });
    }
}

public sealed class ValidationState
{
    public List<GoalField> Fields { get; set; } = new();
} 

public sealed class ValidationFailedData
{
    public string ErrorMessage { get; set; } = string.Empty;
} 