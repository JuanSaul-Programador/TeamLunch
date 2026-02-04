using TeamLunch.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; 
    options.EnableDetailedErrors = true;
});

var builderApp = builder.Build();
var app = builderApp;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBlazorFrameworkFiles(); 
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapHub<VotingHub>("/voting-hub");
app.MapFallbackToFile("index.html"); 
app.Run();