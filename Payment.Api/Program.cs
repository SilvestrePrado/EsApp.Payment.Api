using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Kafka;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment API", Version = "v1" });
});

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

var bootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var requestTopic = builder.Configuration["Kafka:RequestTopic"] ?? "risk-evaluation-request";
var responseTopic = builder.Configuration["Kafka:ResponseTopic"] ?? "risk-evaluation-response";

builder.Services.AddSingleton<IPaymentProducer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PaymentProducer>>();
    return new PaymentProducer(bootstrap, requestTopic, logger);
});

builder.Services.AddHostedService<RiskResponseConsumerService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();