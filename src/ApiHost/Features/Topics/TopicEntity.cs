namespace ApiHost;

public class Topic
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<Todo> Todos { get; set; } = [];
    public int Version { get; set; }

    public Topic()
    {
    }

    public Topic(Guid createdBy, string title)
    {
        Id = Guid.CreateVersion7();
        CreatedBy = createdBy;
        Title = title;
    }

    public TopicEdited Edit(string title)
    {
        Title = title;
        return new TopicEdited(Id, title);
    }

    public TopicDeleted MarkDelete()
    {
        return new TopicDeleted(Id, CreatedBy);
    }

    public TodoAdded AddTodo(string text)
    {
        var todo = new Todo
        {
            Text = text,
        };
        Todos.Add(todo);
        return new TodoAdded(Id, todo.Id, text);
    }

    public TodoEdited EditTodo(Guid todoId, string text)
    {
        var todo = Todos.FirstOrDefault(t => t.Id == todoId);
        if (todo != null)
        {
            todo.Edit(text);
            return new TodoEdited(Id, todoId, text);
        }
        throw new InvalidOperationException($"Todo with id {todoId} not found.");
    }

    public TodoRemoved RemoveTodo(Guid todoId)
    {
        var todo = Todos.FirstOrDefault(t => t.Id == todoId);
        if (todo != null)
        {
            Todos.Remove(todo);
            return new TodoRemoved(Id, todoId);
        }
        throw new InvalidOperationException($"Todo with id {todoId} not found.");
    }

    public TodoCompleted CompleteTodo(Guid todoId)
    {
        var todo = Todos.FirstOrDefault(t => t.Id == todoId);
        if (todo != null)
        {
            todo.Complete();
            return new TodoCompleted(Id, todoId);
        }
        throw new InvalidOperationException($"Todo with id {todoId} not found.");
    }

    public TodoUnchecked UncheckTodo(Guid todoId)
    {
        var todo = Todos.FirstOrDefault(t => t.Id == todoId);
        if (todo != null)
        {
            todo.Uncheck();
            return new TodoUnchecked(Id, todoId);
        }
        throw new InvalidOperationException($"Todo with id {todoId} not found.");
    }
}

public class Todo
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int Version { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? CompletedAt { get; set; } = DateTimeOffset.Now;
    public bool Completed { get; set; }

    public Todo()
    {
    }

    public Todo(string text)
    {
        Text = text;
    }

    public void Edit(string text)
    {
        Text = text;
    }

    public void Complete()
    {
        Completed = true;
        CompletedAt = DateTimeOffset.Now;
    }

    public void Uncheck()
    {
        Completed = false;
        CompletedAt = null;
    }
}

public record TopicCreated(Guid TopicId, Guid CreatedBy, string Title) : BaseEvent;
public record TopicEdited(Guid TopicId, string Title) : BaseEvent;
public record TopicDeleted(Guid TopicId, Guid DeleteBy) : BaseEvent;

public record TodoAdded(Guid TopicId, Guid TodoId, string Text) : BaseEvent;
public record TodoEdited(Guid TopicId, Guid TodoId, string Text) : BaseEvent;
public record TodoRemoved(Guid TopicId, Guid TodoId) : BaseEvent;
public record TodoCompleted(Guid TopicId, Guid TodoId) : BaseEvent;
public record TodoUnchecked(Guid TopicId, Guid TodoId) : BaseEvent;