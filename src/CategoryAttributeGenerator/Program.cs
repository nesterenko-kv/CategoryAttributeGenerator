using CategoryAttributeGenerator.Services;
using CategoryAttributeGenerator.Services.OpenAI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Bind OpenAI options and register HttpClient-based client.
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<IOpenAiClient, OpenAiClient>();

// Prompt options for category attribute generation
builder.Services.Configure<CategoryPromptOptions>(builder.Configuration.GetSection("CategoryPrompting"));

builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Serve static UI from wwwroot (index.html).
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Fallback to index.html for anything else (simple SPA-style routing).
app.MapFallbackToFile("index.html");

app.Run();