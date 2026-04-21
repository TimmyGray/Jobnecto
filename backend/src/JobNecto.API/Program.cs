using JobNecto.API.Infrastructure;
using JobNecto.API.Infrastructure.ExceptionHandling;
using JobNecto.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// `ICookieAuthService` registration deferred to users feature commit to
// keep application registration isolated. Cookie service will be added
// together with the UsersController and its implementation.

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Wire authentication and authorization in the middleware pipeline
// Routing must come first, then authentication/authorization, then endpoints
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
