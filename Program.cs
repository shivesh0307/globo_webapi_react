using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniValidation;
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
    // Console.WriteLine("get to house id");
    var house = await repo.Get(houseId);
    if (house == null)
        return Results.Problem($"House with ID {houseId} not found.", statusCode: 404);
    return Results.Ok(house);
}).ProducesProblem(404).Produces<HouseDetailDto>(StatusCodes.Status200OK);

app.MapPost("/houses", async ([FromBody] HouseDetailDto dto, IHouseRepository repo) =>
{
    if (!MiniValidator.TryValidate(dto, out var errors))
        return Results.ValidationProblem(errors);
    Console.WriteLine("post to house");
    var newHouse = await repo.Add(dto);
    return Results.Created($"/house/{newHouse.Id}", newHouse);

}).ProducesValidationProblem().Produces<HouseDetailDto>(StatusCodes.Status201Created);

/*app.MapPost("/houses", async (HttpRequest Request, IHouseRepository repo) =>
{

    await repo.GetAll();
    Console.WriteLine("post hit");
    Console.WriteLine(Request);


});*/

app.MapPut("/houses", async ([FromBody] HouseDetailDto dto, IHouseRepository repo) =>
{
    if (!MiniValidator.TryValidate(dto, out var errors))
        return Results.ValidationProblem(errors);
    if (await repo.Get(dto.Id) == null)
        return Results.Problem($"House with id {dto.Id} not found ", statusCode: 404);

    var updatedHouse = await repo.Update(dto);
    return Results.Ok(updatedHouse);

}).ProducesValidationProblem().ProducesProblem(404).Produces<HouseDetailDto>(StatusCodes.Status200OK);

app.MapDelete("/houses/{houseId:int}", async (int houseId, IHouseRepository repo) =>
      {
          if (await repo.Get(houseId) == null)
              return Results.Problem($"House with Id {houseId} not found", statusCode: 404);
          await repo.Delete(houseId);
          return Results.Ok();
      }).ProducesProblem(404).Produces(StatusCodes.Status200OK);

app.Run();

