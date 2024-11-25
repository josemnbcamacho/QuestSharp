using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using Spectre.Console;
using System.Text.Json;
using QuestSharp.Models;

#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0003

namespace QuestSharp.Steps;

[KernelProcessStepMetadata("UserInput.V1")]
public class ConsoleStep : KernelProcessStep
{
    public static class Functions
    {
        public const string ProcessInput = nameof(ProcessInput);
        public const string DisplayOutput = nameof(DisplayOutput);
    }

    public static class OutputEvents
    {
        public const string ContinueGoal = nameof(ContinueGoal);
    }

    [KernelFunction(Functions.ProcessInput)]
    public async Task ProcessInputAsync(KernelProcessStepContext context, Kernel kernel)
    {
        var conversationHistory = (List<(string Role, string Content)>)kernel.Data["ConversationHistory"];

        // Check for opener before getting user input
        kernel.Data.TryGetValue("CurrentGoal", out var goal);
        if (goal is Goal currentGoal)
        {
            var isFirstResponse = !kernel.Data.ContainsKey("OpenerDisplayed");
            if (isFirstResponse && !string.IsNullOrEmpty(currentGoal.Opener))
            {
                kernel.Data["OpenerDisplayed"] = true;
                AnsiConsole.MarkupLine("[blue]Assistant:[/] " + EscapeMarkup(currentGoal.Opener));
                
                // Add opener to conversation history
                conversationHistory.Add(("assistant", currentGoal.Opener));
                kernel.Data["ConversationHistory"] = conversationHistory;
            }
        }

        var userInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]User:[/] ")
                .PromptStyle("green"));

        if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "exit")
        {
            Environment.Exit(0);
            return;
        }

        // Add user input to conversation history
        conversationHistory.Add(("user", userInput));
        kernel.Data["ConversationHistory"] = conversationHistory;

        // Continue with current goal
        await context.EmitEventAsync(new() 
        { 
            Id = OutputEvents.ContinueGoal,
            Data = userInput
        });
    }

    [KernelFunction(Functions.DisplayOutput)]
    public async Task DisplayOutputAsync(KernelProcessStepContext context, Kernel kernel, GoalResponse output)
    {
        AnsiConsole.MarkupLine("[blue]Assistant:[/] " + EscapeMarkup(output.Message));
        
        // Display JSON panel for complete responses
        if (output is GoalResponse { Type: GoalResponseType.Complete } completeResponse)
        {
            var json = JsonSerializer.Serialize(completeResponse.Data);
            var panel = new Panel(json)
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader("Result Data")
            };
            panel.BorderColor(Color.Yellow);
            AnsiConsole.Write(panel);
        }
        
        // Add assistant response to conversation history
        var history = (List<(string Role, string Content)>)kernel.Data["ConversationHistory"];
        history.Add(("assistant", output.Message));
        kernel.Data["ConversationHistory"] = history;
        
        // After displaying output, automatically process next input
        await ProcessInputAsync(context, kernel);
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}

public sealed class GoalResponse
{
    public GoalResponseType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
} 