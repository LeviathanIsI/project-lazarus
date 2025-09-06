# Lazarus Data Layer

This project provides the SQLite/Entity Framework Core data persistence layer for the Lazarus application.

## Architecture

### Core Components

- **LazarusDbContext**: Main EF Core DbContext with SQLite optimizations
- **Entities**: Domain models (Conversation, Message, Model, Settings)
- **Repositories**: Repository pattern implementation with async disposal
- **Configurations**: EF Core entity configurations with proper indexes
- **Extensions**: Dependency injection setup and database utilities

### Database Schema

#### Conversations Table
- Id (Guid, PK)
- Title (string, max 255)
- CreatedAt (DateTime)
- LastMessageAt (DateTime)
- Messages (1:N relationship with cascade delete)

#### Messages Table
- Id (Guid, PK)
- ConversationId (Guid, FK)
- Role (MessageRole enum: System, User, Assistant)
- Content (string, unlimited)
- Timestamp (DateTime)
- TokenCount (int, nullable)

#### Models Table
- Id (Guid, PK)
- Name (string, max 255)
- Path (string, max 500)
- RunnerType (RunnerType enum: LlamaCpp, VLlm, ExLlamaV2, Ollama)
- IsActive (bool, default true)
- Parameters (JSON string, nullable)
- CreatedAt (DateTime)
- LastModified (DateTime)

#### Settings Table
- Key (string, max 255, PK)
- Value (string, nullable)
- LastModified (DateTime)

### Performance Optimizations

#### SQLite Settings
- WAL mode for improved concurrency
- 64MB cache size
- Memory-based temp storage
- 256MB memory-mapped I/O

#### Indexes
- Conversations: CreatedAt, LastMessageAt
- Messages: ConversationId, Timestamp, Role
- Models: IsActive (filtered), RunnerType, Name
- Settings: LastModified

## Usage

### Registration
```csharp
// Basic registration with default connection string
services.AddLazarusData();

// Custom connection string
services.AddLazarusData("Data Source=custom.db");

// Custom DbContext configuration
services.AddLazarusData(options => {
    options.UseSqlite(connectionString);
    options.EnableDetailedErrors();
});
```

### Database Initialization
```csharp
// Ensure database exists and apply migrations with seeding
await serviceProvider.EnsureDatabaseAsync(seedData: true);

// Apply SQLite performance optimizations
await serviceProvider.OptimizeSqliteAsync();
```

### Repository Usage
```csharp
// Inject repositories
public class ConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    
    public ConversationService(
        IConversationRepository conversations,
        IMessageRepository messages)
    {
        _conversations = conversations;
        _messages = messages;
    }
    
    public async Task<Conversation?> GetWithMessagesAsync(Guid id)
    {
        return await _conversations.GetConversationWithMessagesAsync(id);
    }
}
```

### Settings Management
```csharp
// Type-safe settings access
var theme = await _settings.GetValueAsync("App.Theme", "Dark");
var autoSave = await _settings.GetValueAsync("App.AutoSave", true);
var maxHistory = await _settings.GetValueAsync("App.MaxConversationHistory", 1000);

// Setting values with type conversion
await _settings.SetValueAsync("App.Theme", "Light");
await _settings.SetValueAsync("App.AutoSave", false);
```

## Default Data

The system seeds with:
- 3 default models (Llama-3.1-8B-Instruct, Mistral-7B-Instruct, CodeLlama-13B-Instruct)
- Common application settings (theme, auto-save, UI dimensions, etc.)

## Database Location

By default, the SQLite database is created at:
`%APPDATA%/Lazarus/lazarus.db`

## Migration Commands

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```