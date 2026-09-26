var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapGet("/api/portfolio", () => new
{
    name = "Abel López",
    title = "Software Engineer",
    tagline = "I build practical digital products with modern frontend and backend tools.",
    focusAreas = new[]
    {
        "Full-stack product development",
        "Clean architecture",
        "Performance and UX",
        "API integration"
    },
    projects = new[]
    {
        new { name = "Cash Harmony", stack = new[] { "Angular", ".NET", "SQL" }, summary = "A financial operations platform focused on process clarity and business efficiency." },
        new { name = "Personal Portfolio", stack = new[] { "Angular", "ASP.NET Core", "Telegram" }, summary = "A personal site to showcase resume, projects and suggestions from visitors." },
        new { name = "Operational Dashboard", stack = new[] { "Angular", "REST API" }, summary = "A dashboard for operational visibility and business metrics." }
    }
});

app.MapControllers();

app.Run();
