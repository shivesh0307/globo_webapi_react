using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", builder =>
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddDbContext<HouseDbContext>(options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.AddScoped<IHouseRepository, HouseRepository>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("MyPolicy");

app.MapGet("/houses", (IHouseRepository repo) =>
{
    return repo.GetAll();
}).Produces<HouseDto[]>(StatusCodes.Status200OK);
app.MapGet("/house/{houseID:int}", async (int houseId, IHouseRepository repo) =>
{
    var house = await repo.Get(houseId);
    if (house == null)
        return Results.Problem($"House with ID {houseId} not found.", statusCode: 404);
    return Results.Ok(house);
}).ProducesProblem(404).Produces<HouseDetailDto>(StatusCodes.Status200OK);

app.MapPost("/house", async ([FromBody] HouseDetailDto dto, IHouseRepository repo) =>
{
    var newHouse = await repo.Add(dto);
    return Results.Created($"/house/{newHouse.Id}", newHouse);

}).Produces<HouseDetailDto>(StatusCodes.Status201Created);

app.MapPut("/house", async ([FromBody] HouseDetailDto dto, IHouseRepository repo) =>
{
    if (await repo.Get(dto.Id) == null)
        return Results.Problem($"House with id {dto.Id} not found ", statusCode: 404);

    var updatedHouse = await repo.Update(dto);
    return Results.Ok(updatedHouse);

}).ProducesProblem(404).Produces<HouseDetailDto>(StatusCodes.Status200OK);

app.MapDelete("/houses/{houseId:int}", async (int houseId, IHouseRepository repo) =>
      {
          if (await repo.Get(houseId) == null)
              return Results.Problem($"House with Id {houseId} not found", statusCode: 404);
          await repo.Delete(houseId);
          return Results.Ok();
      }).ProducesProblem(404).Produces(StatusCodes.Status200OK);

app.Run();

