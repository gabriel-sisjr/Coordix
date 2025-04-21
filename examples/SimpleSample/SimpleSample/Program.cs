using Coordix.Extensions;
using Coordix.Interfaces;
using SimpleSample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adding Coordix
builder.Services.AddMediator();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/PingPong", async (IMediator mediator) =>
{
    var result = await mediator.Send(new Ping { Message = "Hello Mediator" });
    return Results.Ok(result);
});

app.Run();