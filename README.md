# QuestSharp

A goal-oriented conversation flow framework built on top of Microsoft's Semantic Kernel Process Framework, enabling structured and intelligent human-LLM interactions.

## Overview

QuestSharp is a framework that combines the power of Microsoft's Semantic Kernel Process Framework with goal-oriented conversation flows. It allows developers to create sophisticated AI assistants that can:

- Handle complex multi-step conversations with natural language
- Maintain context and state across interactions
- Validate user inputs with custom validation rules
- Route conversations between different goals based on user intent
- Process structured data collection through conversation

## Key Features

- **Goal-Based Architecture**: Define conversation flows as a series of goals with required fields and validation rules
- **Natural Conversation Flow**: LLM-powered natural language processing for fluid interactions
- **Field Validation**: Built-in validation system with custom validators for each field
- **Flexible Routing**: Connect goals together with intent-based transitions and context preservation
- **Process Integration**: Seamless integration with Semantic Kernel's Process Framework
- **State Management**: Maintain conversation history and context across goal transitions
- **Error Recovery**: Graceful handling of validation failures and conversation errors

## Architecture

QuestSharp is built on several key components:

1. **Goals**: Core building blocks representing conversation objectives
   - Name and description for clear identification
   - Required fields with validation rules
   - Opening messages for conversation initiation
   - Connections to other goals for transitions

2. **Process Steps**:
   - `ConsoleStep`: Handles user input/output
   - `GoalPromptStep`: Processes conversation with LLM
   - `ValidationStep`: Validates gathered information
   - `GoalTransitionStep`: Manages transitions between goals
   - `RenderResponseStep`: Formats and delivers responses

3. **Services**:
   - `QuestService`: Orchestrates the conversation flow and maintains state

## Getting Started

1. Define your goals using the builder pattern:
```csharp
var orderGoal = new GoalBuilder()
    .WithName("OrderProcess")
    .WithDescription("Process customer orders")
    .WithOpener("Welcome! Let me help you with your order.")
    .AddField("quantity", "Number of items", val => int.Parse(val.ToString()) > 0)
    .Build();
```

2. Connect goals to create conversation flows:
```csharp
orderGoal.Connect(cancelGoal, "cancel", handOver: true);
orderGoal.Connect(menuGoal, "menu", handOver: true);
```

3. Initialize the service and start processing:
```csharp
var chain = new QuestService(kernel, initialGoal);
await chain.ProcessInputAsync(userInput);
```

## Requirements

- .NET 8.0 SDK or higher
- Azure OpenAI or OpenAI API access

## Documentation

For detailed documentation on the Semantic Kernel Process Framework that powers QuestSharp, visit:
[Process Framework Documentation](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/process/process-framework)

## Acknowledgments

This project is inspired by [GoalChain](https://github.com/adlumal/GoalChain), reimagined and rebuilt using Microsoft's Semantic Kernel Process Framework.

## License

MIT License - See LICENSE file for details 