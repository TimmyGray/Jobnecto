using JobNecto.API.Infrastructure.Cors;
using JobNecto.API.Infrastructure.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

// Use local configs if appsettings.Local.json exists
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add CORS
builder.Services.AddCorsPolicies(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsServiceExtensions.FrontendPolicy);
app.UseHttpsRedirection();

app.Run();
