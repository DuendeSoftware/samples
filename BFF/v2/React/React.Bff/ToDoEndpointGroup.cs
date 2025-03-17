using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace React.Bff;

public static class TodoEndpointGroup
{

    private static readonly List<ToDo> data = new List<ToDo>()
        {
            new ToDo { Id = ToDo.NewId(), Date = DateTimeOffset.UtcNow, Name = "Demo ToDo API", User = "2 (Bob Smith)" },
            new ToDo { Id = ToDo.NewId(), Date = DateTimeOffset.UtcNow.AddHours(1), Name = "Stop Demo", User = "2 (Bob Smith)" },
            new ToDo { Id = ToDo.NewId(), Date = DateTimeOffset.UtcNow.AddHours(4), Name = "Have Dinner", User = "1 (Alice Smith)" },
        };

    public static RouteGroupBuilder ToDoGroup(this RouteGroupBuilder group)
    {
        // GET
        group.MapGet("/", () => data);
        group.MapGet("/{id}", (int id) =>
        {
            var item = data.FirstOrDefault(x => x.Id == id);
        });

        // POST
        group.MapPost("/", (ToDo model, ClaimsPrincipal user, HttpContext context) =>
        {
            model.Id = ToDo.NewId();
            model.User = $"{user.FindFirst("sub")?.Value} ({user.FindFirst("name")?.Value})";
            
            data.Add(model);
            
            var url = new Uri($"{context.Request.GetEncodedUrl()}/{model.Id}");
            
            return Results.Created(url, model);
        });

        // PUT
        group.MapPut("/{id}", (int id, ToDo model, ClaimsPrincipal User) =>
        {
            var item = data.FirstOrDefault(x => x.Id == id);
            if (item == null) return Results.NotFound();

            item.Date = model.Date;
            item.Name = model.Name;

            return Results.NoContent();
        });

        // DELETE
        group.MapDelete("/{id}", (int id) =>
        {
            var item = data.FirstOrDefault(x => x.Id == id);
            if (item == null) return Results.NotFound();

            data.Remove(item);

            return Results.NoContent();
        });

        return group;
    }
}

public class ToDo
{
    static int _nextId = 1;
    public static int NewId()
    {
        return _nextId++;
    }

    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Name { get; set; }
    public string? User { get; set; }
}