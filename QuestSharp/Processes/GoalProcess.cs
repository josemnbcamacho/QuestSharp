using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using QuestSharp.Models;
using QuestSharp.Steps;

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

namespace QuestSharp.Processes;

public static class GoalProcess
{
    public static class ProcessEvents
    {
        public const string StartGoal = nameof(StartGoal);
        public const string GoalCompleted = nameof(GoalCompleted);
        public const string ValidationError = nameof(ValidationError);
        public const string TransitionRequested = nameof(TransitionRequested);
        public const string TransitionCompleted = nameof(TransitionCompleted);
    }

    public static ProcessBuilder CreateProcess(Goal goal)
    {
        var processBuilder = new ProcessBuilder($"{goal.Name}Process");
        
        // Add all steps
        var userInputStep = processBuilder.AddStepFromType<ConsoleStep>();
        var promptStep = processBuilder.AddStepFromType<GoalPromptStep>();
        var validationStep = processBuilder.AddStepFromType<ValidationStep>();
        var transitionStep = processBuilder.AddStepFromType<GoalTransitionStep>();
        var renderResponseStep = processBuilder.AddStepFromType<RenderResponseStep>();

        // Define process flow
        processBuilder
            .OnInputEvent(ProcessEvents.StartGoal)
            .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, 
                functionName: ConsoleStep.Functions.ProcessInput));

        // Handle user input outcomes
        userInputStep
            .OnEvent(ConsoleStep.OutputEvents.ContinueGoal)
            .SendEventTo(new ProcessFunctionTargetBuilder(promptStep,
                functionName: GoalPromptStep.Functions.EvaluateInput));

        // Handle prompt outcomes
        promptStep
            .OnEvent(GoalPromptStep.OutputEvents.Continue)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderResponseStep,
                functionName: RenderResponseStep.Functions.RenderResponse));

        promptStep
            .OnEvent(GoalPromptStep.OutputEvents.Completed)
            .SendEventTo(new ProcessFunctionTargetBuilder(validationStep,
                functionName: ValidationStep.Functions.ValidateJson));
        
        promptStep
            .OnEvent(GoalPromptStep.OutputEvents.TransitionRequested)
            .SendEventTo(new ProcessFunctionTargetBuilder(transitionStep,
                functionName: GoalTransitionStep.Functions.HandleTransition));

        validationStep
            .OnEvent(ValidationStep.OutputEvents.ValidationPassed)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderResponseStep,
                functionName: RenderResponseStep.Functions.RenderResponse));

        validationStep
            .OnEvent(ValidationStep.OutputEvents.ValidationFailed)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderResponseStep,
                functionName: RenderResponseStep.Functions.RenderResponse));

        // Handle transitions
        transitionStep
            .OnEvent(GoalTransitionStep.OutputEvents.TransitionAccepted)
            .SendEventTo(new ProcessFunctionTargetBuilder(promptStep,
                functionName: GoalPromptStep.Functions.EvaluateInput));

        // Connect RenderResponseStep to UserInputStep for display
        renderResponseStep
            .OnEvent(RenderResponseStep.OutputEvents.ResponseReady)
            .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep,
                functionName: ConsoleStep.Functions.DisplayOutput));

        return processBuilder;
    }
} 