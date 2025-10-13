using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure;
using Inventory.Domain;
using Inventory.App;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()     // or .WithOrigins("http://localhost:5173") for Vue
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// App services
builder.Services.AddSingleton<IPubSub, InMemoryPubSub>();

builder.Services.AddScoped<IReorderService, ReorderService>();
;

// Web API
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<InventoryMonitor>();
builder.Services.AddHostedService<DemandSimulator>();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");



app.MapControllers();

app.MapGet("/", () => "Hello World!");
await Seed.EnsureAsync(app);

app.Run("http://localhost:5000");


