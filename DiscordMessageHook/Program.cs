using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", x =>
    {
           x.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();
app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.MapPost("/sendMessage", async (HttpContext context, string webhookId) =>
{
    var token = app.Configuration[$"Tokens:{webhookId}"];
    if(token == null)
    {
        app.Logger.LogInformation("Unauthorized request received for {webhookId}", webhookId);
        return Results.Unauthorized();
    }
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();
    app.Logger.LogInformation("Received Message from {webhookId}", webhookId);
    app.Logger.LogInformation("Forwarding content: {json}", json);
    using var httpClient = new HttpClient();
    var uri = new Uri($"https://discord.com/api/webhooks/{webhookId}/{token}?wait=true");
    var result = await httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
    var responseBody = await result.Content.ReadAsStringAsync();
    Console.WriteLine(responseBody);
    return Results.Content(responseBody, result.Content.Headers.ContentType?.MediaType, statusCode: (int)result.StatusCode);
});
var port = app.Configuration["Port"] ?? "4500";
var ip = app.Configuration["IP"] ?? "127.0.0.1";
app.Urls.Add($"http://{ip}:{port}");
app.Run();