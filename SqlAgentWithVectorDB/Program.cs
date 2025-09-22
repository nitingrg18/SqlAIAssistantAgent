#region Commented Code

//using SqlAgentWithVectorDB.Models;
//using SqlAgentWithVectorDB.Services;
//using SqlAgentWithVectorDB.Services.Interfaces;

//var builder = WebApplication.CreateBuilder(args);

//// 1. Configure CORS
//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
//builder.Services.AddCors(options => {
//    options.AddPolicy(name: MyAllowSpecificOrigins, policy => {
//        policy.WithOrigins("http://localhost:3000") // Allow React dev server
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

//// 2. Add standard API services
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddControllers();

//// 3. Configure and register our custom Assistant Service
//var openAiApiKey = builder.Configuration["OpenAIApiKey"];
//if (string.IsNullOrEmpty(openAiApiKey) || openAiApiKey.Contains("Your-Key"))
//{
//    Console.ForegroundColor = ConsoleColor.Red;
//    Console.WriteLine("FATAL ERROR: OpenAI API Key is not configured in appsettings.json.");
//    Console.ResetColor();
//}

//// Register the service using the interface for dependency injection
//builder.Services.AddSingleton<IAssistantService, AssistantService>(serviceProvider =>
//{
//    var service = new AssistantService(openAiApiKey!);
//    var schema = GetDatabaseSchema();
//    // Initialize the assistant on startup
//    service.InitializeAssistantAsync(schema).GetAwaiter().GetResult();
//    return service;
//});

//var app = builder.Build();

//// 4. Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseHttpsRedirection();
//app.UseCors(MyAllowSpecificOrigins); // Use the CORS policy
//app.UseAuthorization();
//app.MapControllers();
//app.Run();


//// This helper function defines our mock database schema.
//static List<TableSchema> GetDatabaseSchema()
//{
//    return new List<TableSchema>
//    {
//        new() {
//            Name = "Users", Description = "Stores user account information.", Columns = new List<ColumnSchema> {
//                new() { Name = "UserID", Type = "INT", Description = "Unique identifier for each user." },
//                new() { Name = "Username", Type = "VARCHAR", Description = "The user's login name." },
//                new() { Name = "SignupDate", Type = "DATETIME", Description = "The date the user registered." }
//            }
//        },
//        new() {
//            Name = "Products", Description = "Stores information about products available for sale.", Columns = new List<ColumnSchema> {
//                new() { Name = "ProductID", Type = "INT", Description = "Unique identifier for each product." },
//                new() { Name = "ProductName", Type = "VARCHAR", Description = "The name of the product." },
//                new() { Name = "Price", Type = "DECIMAL", Description = "The price of the product." }
//            }
//        },
//        new() {
//            Name = "Orders", Description = "Stores customer orders. Links users and products.", Columns = new List<ColumnSchema> {
//                new() { Name = "OrderID", Type = "INT", Description = "Unique identifier for each order." },
//                new() { Name = "UserID", Type = "INT", Description = "Foreign key referencing the Users table." },
//                new() { Name = "ProductID", Type = "INT", Description = "Foreign key referencing the Products table." },
//                new() { Name = "OrderDate", Type = "DATETIME", Description = "The date the order was placed." },
//                new() { Name = "Quantity", Type = "INT", Description = "Number of items of the product ordered." }
//            }
//        }
//    };
//}

#endregion



using FluentValidation;
using MediatR;
using SqlAgent.Application.Behaviors;
using SqlAgent.Application.Interfaces;
using SqlAgent.Infrastructure.AiServices;
using SqlAgent.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options => {
    options.AddPolicy(name: MyAllowSpecificOrigins, policy => {
        policy.WithOrigins("http://localhost:3000") // Allow React dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SqlAgent.Application.IAssemblyMarker>());
builder.Services.AddValidatorsFromAssemblyContaining<SqlAgent.Application.IAssemblyMarker>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<ISchemaRepository, SchemaRepository>();
builder.Services.AddSingleton<IAssistantService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var apiKey = configuration["OpenAIApiKey"];

    if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("Your-Key"))
        throw new InvalidOperationException("OpenAI API Key is not configured in appsettings.json.");

    using (var scope = serviceProvider.CreateScope())
    {
        var schemaRepository = scope.ServiceProvider.GetRequiredService<ISchemaRepository>();
        var schema = schemaRepository.GetSchemaAsync().GetAwaiter().GetResult();

        var service = new AssistantService(apiKey, configuration);
        service.InitializeAssistantAsync(schema).GetAwaiter().GetResult();
        return service;
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();
app.Run();