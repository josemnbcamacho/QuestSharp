using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using System.Text.Json;
using QuestSharp.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0010

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

[KernelProcessStepMetadata("GoalPrompt.V1")]
public class GoalPromptStep : KernelProcessStep
{
    private const string EvaluatePromptTemplate = @"
You are an AI assistant tasked with a specific goal-oriented conversation. Your role is to naturally guide the conversation while staying focused on gathering required information.

GOAL: {{$goal}}

Information to be gathered:
{{$required_fields}}

Available transitions (only if explicitly requested):
{{$connected_goals}}

Instructions:
1. Maintain a natural, friendly conversation without being repetitive
2. Only gather the specified information - do not ask for anything outside the required fields
3. If a user input query demonstrates intent that would be responded by a connected goal, facilitate that transition
4. Once all required information is gathered, extract and format it as specified

Evaluate the conversation and respond in the following JSON format (without markdown tags):
{
    ""isComplete"": true/false,
    ""missingFields"": [""field1"", ""field2""],
    ""extractedData"": {
        // field-value pairs for gathered information
    },
    ""nextPrompt"": ""what to ask the user next"",
    ""transition"": {
        ""targetGoal"": ""goalName"",
        ""handOver"": true/false
    }
}

Notes:
- Set isComplete to true only when ALL required information is gathered.
- Include clear, natural language in nextPrompt to gather missing information.
- Keep responses concise and focused on the goal.
- Respond naturally, and don't repeat yourself.0
- Always respond in JSON format - failure to do so will result in an error.

Conversation so far:
{{$conversation}}";

    public static class Functions
    {
        public const string EvaluateInput = nameof(EvaluateInput);
    }

    public static class OutputEvents
    {
        public const string Continue = nameof(Continue);
        public const string Completed = nameof(Completed);
        public const string TransitionRequested = nameof(TransitionRequested);
    }

    [KernelFunction(Functions.EvaluateInput)]
    public async Task EvaluateInputAsync(KernelProcessStepContext context, Kernel kernel, string userInput)
    {
        kernel.Data.TryGetValue("CurrentGoal", out var goal);
        kernel.Data.TryGetValue("ConversationHistory", out var historyObj);
        
        if (goal is not Goal currentGoal)
        {
            throw new InvalidOperationException("Current goal not found in kernel data.");
        }

        var history = (List<(string Role, string Content)>)historyObj;

        // Prepare prompt arguments
        var args = new KernelArguments(new OpenAIPromptExecutionSettings()
        {
            ResponseFormat = "json_object"
        })
        {
            ["goal"] = currentGoal.Description,
            ["required_fields"] = FormatRequiredFields(currentGoal.Fields),
            ["conversation"] = FormatConversationHistory(history),
            ["connected_goals"] = FormatConnectedGoals(currentGoal.Connections)
        };

        // Evaluate using SK
        var evaluationFunction = kernel.CreateFunctionFromPrompt(EvaluatePromptTemplate);
        var result = await evaluationFunction.InvokeAsync(kernel, args);
        
        try
        {
            var evaluation = JsonSerializer.Deserialize<GoalEvaluation>(
                result.ToString(), 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (evaluation?.IsComplete == true)
            {
                await context.EmitEventAsync(new() 
                { 
                    Id = OutputEvents.Completed,
                    Data = evaluation.ExtractedData
                });
            }
            else if (evaluation?.Transition != null && !string.IsNullOrEmpty(evaluation.Transition.TargetGoal))
            {
                var transitionGoal = currentGoal.Connections.FirstOrDefault(g => g.TargetGoal.Name == evaluation.Transition.TargetGoal);
                
                if (transitionGoal == null)
                {
                    throw new InvalidOperationException("Invalid transition goal specified.");
                }
                
                await context.EmitEventAsync(new()
                {
                    Id = OutputEvents.TransitionRequested,
                    Data = new GoalTransitionData()
                    {
                        Goal = transitionGoal?.TargetGoal,
                        UserInput = userInput
                    }
                });
            }
            else
            {
                history.Add(("assistant", evaluation?.NextPrompt ?? "Could you provide more information?"));
                kernel.Data["ConversationHistory"] = history;
                await context.EmitEventAsync(new() 
                { 
                    Id = OutputEvents.Continue,
                    Data = history.Last().Content
                });
            }
        }
        catch (Exception)
        {
            history.Add(("assistant", "I'm having trouble processing that. Could you try again?"));
            kernel.Data["ConversationHistory"] = history;
            await context.EmitEventAsync(new() 
            { 
                Id = OutputEvents.Continue,
                Data = history.Last().Content
            });
        }
    }

    private static string FormatRequiredFields(List<GoalField> fields)
    {
        return string.Join("\n", fields.Select(f => $"- {f.Description} (field: {f.Name})"));
    }

    private static string FormatConversationHistory(List<(string Role, string Content)> history)
    {
        return string.Join("\n", history.Select(h => $"{h.Role}: {h.Content}"));
    }
    
    private static string FormatConnectedGoals(List<GoalConnection> connections)
    {
        return string.Join("\n", connections.Select(c => $"- Intent:{c.UserIntent} -> Goal Name:{c.TargetGoal.Name}"));
    }
}

public class GoalEvaluation
{
    public bool IsComplete { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public Dictionary<string, object> ExtractedData { get; set; } = new();
    public string NextPrompt { get; set; } = string.Empty;
    public GoalTransition Transition { get; set; } = null;
}

public class GoalTransition
{
    public string TargetGoal { get; set; } = string.Empty;
    public bool HandOver { get; set; }
}

