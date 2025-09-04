using Lazarus.App.Data.Extensions;
using Lazarus.App.Orchestrator.Services;
using Lazarus.App.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Lazarus Orchestrator API", Version = "v1" });
});

// Add data services
builder.Services.AddDataServices(builder.Configuration);

// Add business services
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddSingleton<IRunnerService, RunnerService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Ensure database is created
await app.Services.EnsureDatabaseCreatedAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lazarus Orchestrator API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();