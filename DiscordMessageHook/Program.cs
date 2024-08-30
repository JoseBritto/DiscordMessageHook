using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
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
app.Run();