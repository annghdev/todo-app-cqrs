using Marten.Events.Projections;

namespace ApiHost;

public class UserView
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TopicCount { get; set; }
}

public class UserProjection : MultiStreamProjection<UserView, Guid>
{
    public UserProjection()
    {
        Identity<UserCreated>(x => x.UserId);
        Identity<UserFullNameChanged>(x => x.UserId);
        Identity<UserDisplayNameChanged>(x => x.UserId);
        Identity<UserDeleted>(x => x.UserId);
        Identity<TopicCreated>(x => x.CreatedBy);
        Identity<TopicDeleted>(x => x.DeleteBy);
    }

    public void Apply(UserView view, UserCreated evt)
    {
        view.FullName = evt.FullName;
        view.DisplayName = evt.DisplayName;
    }

    public void Apply(UserView view, UserFullNameChanged evt)
    {
        view.FullName = evt.FullName;
        view.DisplayName = evt.DisplayName;
    }

    public void Apply(UserView view, UserDisplayNameChanged evt)
    {
        view.DisplayName = evt.DisplayName;
    }

    public bool ShouldDelete(UserView view, UserDeleted evt)
    {
        return true;
    }

    public void Apply(UserView view, TopicCreated evt)
    {
        view.TopicCount++;
    }
    public void Apply(UserView view, TopicDeleted evt)
    {
        view.TopicCount--;
    }
}
