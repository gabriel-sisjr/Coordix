using AdvancedSample.Application;
using AdvancedSample.Service.Email;
using Coordix.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Dependency Injection of your project Modules.
builder.Services.AddApplicationModule();
builder.Services.AddServiceEmailModule();

// Registering Coordix
// The method with same declaration to minimize migration effort!
builder.Services.AddMediator();
// Or
//builder.Services.AddCoordix(); // Our own declaration! :D

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
