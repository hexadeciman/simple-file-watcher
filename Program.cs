using FileStreamer.Auth;
using FileStreamer.Endpoints;
using FileStreamer.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddJsonStreaming(builder.Configuration);
builder.Services.AddSupabaseJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapJsonFileEndpoints();
app.MapJsonWebSocketEndpoints();

app.Run();
