using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using QuestSharp.Models;

namespace QuestSharp;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", true)
            .Build();

        var openAiConfig = configuration.GetSection("AzureOpenAI");

        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();
        
        // Add Azure OpenAI chat completion capability
        builder.AddAzureOpenAIChatCompletion(
            openAiConfig["Model"] ?? "gpt-4o-mini",
            openAiConfig["Endpoint"] ?? throw new ArgumentNullException("AzureOpenAI:Endpoint"),
            openAiConfig["Key"] ?? throw new ArgumentNullException("AzureOpenAI:Key"),
            openAiConfig["ServiceId"] ?? "azure-openai"
        );
        
        var kernel = builder.Build();
        
        // Shop entry goal - initial state
        var shopEntryGoal = new GoalBuilder()
            .WithName("FoodShop")
            .WithDescription("Welcome customers and help them choose between pizza or tacos")
            .WithOpener("Welcome to our Food Delivery App! Would you like to order pizza or tacos today?")
            .Build();

        // Pizza order goal
        var pizzaOrderGoal = new GoalBuilder()
            .WithName("PizzaOrder")
            .WithDescription("Complete pizza order with customization and delivery options")
            .WithOpener("Great choice! Let's customize your perfect pizza.")
            .AddField("size", "Pizza size options: Small, Medium, Large",
                (val) => new[] {"small", "medium", "large"}.Contains(val.ToString().ToLower()))
            .AddField("crust", "Available crust types: Thin, Regular, Thick, Stuffed",
                (val) => new[] {"thin", "regular", "thick", "stuffed"}.Contains(val.ToString().ToLower()))
            .AddField("toppings", "Desired pizza toppings (comma-separated list)")
            .AddField("quantity", "Number of pizzas (1-5)",
                (val) => int.TryParse(val.ToString(), out var qty) && qty > 0 && qty <= 5)
            .AddField("wantDrinks", "Drink order preference",
                (val) => new[] {"yes", "no"}.Contains(val.ToString().ToLower()))
            .AddField("drinks", "Selected drinks (comma-separated list)")
            .AddField("deliveryType", "Order fulfillment method: pickup or delivery",
                (val) => new[] {"pickup", "delivery"}.Contains(val.ToString().ToLower()))
            .AddField("address", "Delivery location address")
            .AddField("phoneNumber", "Contact phone number")
            .Build();

        // Taco order goal
        var tacoOrderGoal = new GoalBuilder()
            .WithName("TacoOrder")
            .WithDescription("Complete taco order with customization, sides, drinks, and delivery")
            .WithOpener("¡Bienvenidos! Welcome to TacoChain! I'll help you order some delicious tacos.")
            .AddField("quantity", "Number of tacos (1-10)", 
                (val) => int.TryParse(val.ToString(), out var qty) && qty > 0 && qty <= 10)
            .AddField("meat", "Meat selection options: Carne Asada, Pollo, Pescado, Al Pastor, Chorizo", 
                (val) => new[] {"carne asada", "pollo", "pescado", "al pastor", "chorizo"}.Contains(val.ToString().ToLower()))
            .AddField("tortilla", "Tortilla type: corn or flour",
                (val) => new[] {"corn", "flour"}.Contains(val.ToString().ToLower()))
            .AddField("toppings", "Selected toppings from: onions, cilantro, salsa, guacamole, cheese (comma-separated list)")
            .AddField("wantSides", "Side dish preference",
                (val) => new[] {"yes", "no"}.Contains(val.ToString().ToLower()))
            .AddField("sides", "Selected sides from: rice, beans, chips & salsa, guacamole (comma-separated list)")
            .AddField("wantDrinks", "Beverage preference",
                (val) => new[] {"yes", "no"}.Contains(val.ToString().ToLower()))
            .AddField("drinks", "Selected drinks from: Mexican Coca-Cola, Jarritos, Horchata, Mexican beer (comma-separated list)")
            .AddField("deliveryType", "Order fulfillment method: pickup or delivery",
                (val) => new[] {"pickup", "delivery"}.Contains(val.ToString().ToLower()))
            .AddField("address", "Delivery location address")
            .AddField("phoneNumber", "Contact phone number")
            .Build();

        // Order cancellation goal
        var cancelGoal = new GoalBuilder()
            .WithName("CancelOrder")
            .WithDescription("Process order cancellation")
            .WithOpener("I understand you want to cancel your order. Could you please tell me the reason for cancellation?")
            .AddField("reason", "Order cancellation reason")
            .Build();

        // Connect goals
        shopEntryGoal.Connect(pizzaOrderGoal, "pizza", handOver: true);
        shopEntryGoal.Connect(tacoOrderGoal, "tacos", handOver: true);
        
        pizzaOrderGoal.Connect(cancelGoal, "cancel", handOver: true, keepMessages: true);
        pizzaOrderGoal.Connect(shopEntryGoal, "menu", handOver: true);
        
        tacoOrderGoal.Connect(cancelGoal, "cancel", handOver: true, keepMessages: true);
        tacoOrderGoal.Connect(shopEntryGoal, "menu", handOver: true);

        Console.WriteLine("🍕 🌮 Welcome to our Food Delivery App - Type 'exit' to quit, 'cancel' to cancel order, or 'menu' to return to main menu");
        Console.WriteLine("Starting your order...\n");

        // Create chain and start with the shop entry goal
        var chain = new QuestService(kernel, shopEntryGoal);
        await chain.ProcessInputAsync(string.Empty);
    }
}