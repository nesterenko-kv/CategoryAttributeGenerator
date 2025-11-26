using CategoryAttributeGenerator.Services;
using CategoryAttributeGenerator.Services.OpenAI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind OpenAI options and register HttpClient-based client.
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<IOpenAiClient, OpenAiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Prompt options for category attribute generation
builder.Services.Configure<CategoryPromptOptions>(
    builder.Configuration.GetSection("CategoryPrompting"));

builder.Services.Configure<CategoryAttributesOptions>(
    builder.Configuration.GetSection("CategoryAttributes"));

builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static UI from wwwroot (index.html).
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Fallback to index.html for anything else (simple SPA-style routing).
app.MapFallbackToFile("index.html");

app.Run();