using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PaymentsApi;
using PaymentsApi.Consumers;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqVirtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? string.Empty;
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? string.Empty;

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            rabbitMqHost,
            rabbitMqVirtualHost,
            h =>
            {
                h.Username(rabbitMqUsername);
                h.Password(rabbitMqPassword);
            });

        cfg.ConfigureJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = null;
            options.Converters.Add(new DecimalStringConverter());
            return options;
        });

        cfg.ReceiveEndpoint("fcg.payments.order-placed", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("fcg.order-placed-event");
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });

        cfg.Message<PaymentProcessedEvent>(m =>
        {
            m.SetEntityName("fcg.payment-processed-event");
        });
    });
});

builder.Services.AddHostedService<Worker>();
builder.Services.AddHealthChecks()
    .AddRabbitMQ(_ =>
    {
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = rabbitMqHost,
            VirtualHost = rabbitMqVirtualHost,
            UserName = rabbitMqUsername,
            Password = rabbitMqPassword
        };

        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    }, name: "rabbitmq");

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("/", () => Results.Ok(new { service = "payments-api", status = "running" }));

app.Run();
