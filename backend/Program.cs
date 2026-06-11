using backend.Models.Enviroments;
using backend.Queue.CorteQueue;
using backend.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MinIoOptions>(builder.Configuration.GetSection("MinIO"));

builder.Services.AddSingleton<ICorteQueue,CorteQueue>();
builder.Services.AddHostedService<CorteVideoWorker>();
builder.Services.AddControllers();
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
