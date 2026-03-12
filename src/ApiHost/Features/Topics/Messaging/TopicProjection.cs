using Marten.Events.Projections;

namespace ApiHost;

public class TopicProjection : MultiStreamProjection<TopicView, Guid>
{
    public TopicProjection()
    {
        Identity<TopicCreated>(x => x.TopicId);
        Identity<TopicEdited>(x => x.TopicId);
        Identity<TopicDeleted>(x => x.TopicId);
        Identity<TodoAdded>(x => x.TopicId);
        Identity<TodoEdited>(x => x.TopicId);
        Identity<TodoRemoved>(x => x.TopicId);
        Identity<TodoCompleted>(x => x.TopicId);
        Identity<TodoUnchecked>(x => x.TopicId);
    }

    public void Apply(TopicCreated e, TopicView view)
    {
        view.Title = e.Title;
        view.CreatedBy = e.CreatedBy;
        view.CreatedAt = DateTimeOffset.Now;
    }
    public void Apply(TopicEdited e, TopicView view)
    {
        view.Title = e.Title;
    }
    public bool ShouldDelete(TopicDeleted e, TopicView view)
    {
        return true;
    }
    public void Apply(TodoAdded e, TopicView view)
    {
        view.Todos.Add(new TodoView { Id = e.TodoId, Text = e.Text });
        view.TotalTodoCount++;
    }
    public void Apply(TodoEdited e, TopicView view)
    {
        var todo = view.Todos.FirstOrDefault(t => t.Id == e.TodoId);
        if (todo != null)
        {
            todo.Text = e.Text;
        }
    }
    public void Apply(TodoRemoved e, TopicView view)
    {
        var todo = view.Todos.FirstOrDefault(t => t.Id == e.TodoId);
        if (todo != null)
        {
            view.Todos.Remove(todo);
            view.TotalTodoCount--;
        }
    }
    public void Apply(TodoCompleted e, TopicView view)
    {
        var todo = view.Todos.FirstOrDefault(t => t.Id == e.TodoId);
        if (todo != null)
        {
            todo.Complete = true;
        }
    }
    public void Apply(TodoUnchecked e, TopicView view)
    {
        var todo = view.Todos.FirstOrDefault(t => t.Id == e.TodoId);
        if (todo != null)
        {
            todo.Complete = false;
        }
    }
}
public class TopicView
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalTodoCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<TodoView> Todos { get; set; } = [];
}

public class TodoView
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Complete { get; set; }
}