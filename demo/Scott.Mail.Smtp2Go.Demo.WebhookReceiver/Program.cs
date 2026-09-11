// Minimal SMTP2GO callback receiver for Scott.Mail.Smtp2Go.AspNetCore: one Map call, typed handlers, optional bearer token and source-IP check.
//
//   dotnet run --project demo/Scott.Mail.Smtp2Go.Demo.WebhookReceiver
//   curl -i -X POST http://localhost:5080/webhooks/smtp2go -H "Content-Type: application/json" --data @test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures/Webhooks/Live/bounce-hard.json
//   curl -i -X POST http://localhost:5080/webhooks/smtp2go -H "Content-Type: application/x-www-form-urlencoded" --data @test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures/Webhooks/Live/delivered.form
//
// Set Smtp2Go:Webhook:BearerToken (appsettings.json, user secrets or the Smtp2Go__Webhook__BearerToken variable) to the webhook's auth_header_value to
// require it, and Smtp2Go:Webhook:RequireSourceIp=true to accept only webhooks.smtp2go.com's addresses (behind a proxy, configure ForwardedHeaders first).
using Scott.Mail.Smtp2Go.AspNetCore;
using Scott.Mail.Smtp2Go.Demo.WebhookReceiver;
using Scott.Mail.Smtp2Go.Webhooks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// The dispatcher, the source-IP resolver and the options. Handlers are registered explicitly: one per event type, or a catch-all on WebhookEvent.
builder.Services.AddSmtp2GoWebhooks(options => options.KnownCustomHeaders.Add("X-Customer-Id"));
builder.Services.AddSingleton<IWebhookEventHandler<WebhookEvent>, LoggingHandler>();
builder.Services.AddSingleton<IWebhookEventHandler<EmailBounceEvent>, BounceHandler>();

WebApplication app = builder.Build();

// Callbacks of every type go to the registered handlers: BounceHandler first for bounces, then LoggingHandler for everything.
Smtp2GoWebhookEndpointConventionBuilder webhook = app.MapSmtp2GoWebhook("/webhooks/smtp2go");

string? bearerToken = builder.Configuration["Smtp2Go:Webhook:BearerToken"];
if (!string.IsNullOrEmpty(bearerToken))
{
    webhook.RequireBearer(bearerToken);
}

if (builder.Configuration.GetValue<bool>("Smtp2Go:Webhook:RequireSourceIp"))
{
    webhook.RequireSmtp2GoSourceIp();
}

// The same parser without handler registration: an inline delegate is enough for a single-purpose endpoint.
app.MapSmtp2GoWebhook("/webhooks/smtp2go/inline", (webhookEvent, _) =>
{
    app.Logger.LogInformation("Inline endpoint received {Kind} ({EventRaw})", webhookEvent.Kind, webhookEvent.EventRaw);
    return Task.CompletedTask;
});

app.MapGet("/", () => "POST SMTP2GO callbacks (JSON or form) to /webhooks/smtp2go");

app.Run();
