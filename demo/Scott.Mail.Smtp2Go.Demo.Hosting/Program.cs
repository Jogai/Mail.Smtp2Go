// Generic-host sample for Scott.Mail.Smtp2Go.DependencyInjection: one call registers a validated, factory-managed, resilient client.
// Keys come from appsettings.json, user secrets (dotnet user-secrets set "Smtp2Go:ApiKey" "api-...") or environment variables (Smtp2Go__ApiKey).
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scott.Mail.Smtp2Go;
using Scott.Mail.Smtp2Go.DependencyInjection;

// Content root next to the binary so appsettings.json is found however the sample is started (dotnet run, or the executable from any directory).
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });

// Default client bound from the "Smtp2Go" section; the resilience pipeline and logging are attached automatically.
builder.Services.AddSmtp2Go(builder.Configuration.GetSection("Smtp2Go"));

// A second, named client with its own key and region, resolved through ISmtp2GoClientFactory or [FromKeyedServices("marketing")].
builder.Services.AddSmtp2Go("marketing", builder.Configuration.GetSection("Smtp2Go:Marketing"));

using IHost host = builder.Build();
await host.StartAsync();   // ValidateOnStart runs here: a missing key fails with "Smtp2Go:ApiKey is required."

ISmtp2GoClient client = host.Services.GetRequiredService<ISmtp2GoClient>();
ISmtp2GoClient marketing = host.Services.GetRequiredService<ISmtp2GoClientFactory>().Create("marketing");
ISmtp2GoClient keyed = host.Services.GetRequiredKeyedService<ISmtp2GoClient>("marketing");

Console.WriteLine($"Default client region: {((Smtp2GoClient)client).Options.Region}");
Console.WriteLine($"Marketing client region: {((Smtp2GoClient)marketing).Options.Region} (keyed resolution gives the same options: {((Smtp2GoClient)keyed).Options.Region})");

string apiKey = ((Smtp2GoClient)client).Options.ApiKey!;
if (Smtp2GoClientOptions.IsWellFormedApiKey(apiKey))
{
    // stats/email_cycle is idempotent, so a 503 or 429 from the API would be retried by the pipeline.
    using JsonDocument cycle = await client.Raw.SendJsonAsync("stats/email_cycle", default);
    Console.WriteLine($"Current cycle (request {cycle.RootElement.GetProperty("request_id")}): {cycle.RootElement.GetProperty("data")}");
}
else
{
    Console.WriteLine("Smtp2Go:ApiKey is a placeholder; set a real key to make a live call. Registration, validation and resolution succeeded.");
}

await host.StopAsync();
