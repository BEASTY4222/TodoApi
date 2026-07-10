using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseSqlite("Data Source=Todo.db"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Blazor",
        policy =>
        {
            policy.WithOrigins("http://localhost:5259")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config => {config.DocumentTitle = "TodoAPI";
                                config.Path = "/swagger";
                                config.DocumentPath = "/swagger/{documentName}/swagger.json";
                                config.DocExpansion = "List";
                                });
}

var todoItems = app.MapGroup("/todoItems");

todoItems.MapGet("/",GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);

app.Run();

static async Task<IList<string>> GetAllTodos(TodoDb db)
{
    List<string> result = new List<string>();

    List<Todo> Todos = await db.Todos.ToListAsync();
    foreach (Todo elem in Todos)
    {
        string done = elem.IsComplete == true ? "done" : "not finished";
        result.Add($"ID:{elem.Id} TASK-- {elem.Name} is {done} --");
    }

    return result;
}

//static async Task<IResult> GetCompleteTodos(TodoDb db) => TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).ToListAsync());
static async Task<IList<string>> GetCompleteTodos(TodoDb db)
{
    List<string> result = new List<string>();

    List<Todo> Todos = await db.Todos.ToListAsync();
    foreach (Todo elem in Todos)
    {
        if(elem.IsComplete == true)
        {
            result.Add($"ID:{elem.Id} TASK-- {elem.Name} is done --");
        }
    }

    return result;
}

static async Task<IResult> GetTodo(int id, TodoDb db) => await db.Todos.FindAsync(id) is Todo todo ? TypedResults.Ok(todo) : TypedResults.NotFound();

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db)
{
    Todo todo = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name
    };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/todoItems/{todo.Id}", todo);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db)
{
    Todo todo = await db.Todos.FindAsync(id);

    if(todo == null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;
    
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if(await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}
