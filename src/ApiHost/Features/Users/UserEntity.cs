namespace ApiHost;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public DisplayNameFormat DisplayNameFormat { get; set; } = DisplayNameFormat.FirstNameLastName;
    public string FullName => $"{FirstName} {LastName}";
    public string DisplayName => DisplayNameFormat switch
    {
        DisplayNameFormat.FirstNameLastName => $"{FirstName} {LastName}",
        DisplayNameFormat.LastNameFirstName => $"{LastName} {FirstName}",
        DisplayNameFormat.FirstNameOnly => FirstName,
        DisplayNameFormat.LastNameOnly => LastName,
        _ => $"{FirstName} {LastName}"
    };

    public User()
    {
    }

    public User(string firstName, string lastName)
    {
        Id = Guid.CreateVersion7();
        FirstName = firstName;
        LastName = lastName;
    }

    public User(string firstName, string lastName, DisplayNameFormat displayNameFormat)
    {
        Id = Guid.CreateVersion7();
        FirstName = firstName;
        LastName = lastName;
        DisplayNameFormat = displayNameFormat;
    }

    public UserFullNameChanged UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        return new UserFullNameChanged(Id, FullName, DisplayName);
    }

    public UserDisplayNameChanged UpdateDisplayNameFormat(DisplayNameFormat displayNameFormat)
    {
        DisplayNameFormat = displayNameFormat;
        return new UserDisplayNameChanged(Id, DisplayName);
    }

    public UserDeleted MarkDelete()
    {
        return new UserDeleted(Id);
    }
}

public enum DisplayNameFormat
{
    FirstNameLastName,
    LastNameFirstName,
    FirstNameOnly,
    LastNameOnly,
}

public record UserCreated(Guid UserId, string DisplayName, string FullName) : BaseEvent;
public record UserFullNameChanged(Guid UserId, string FullName, string DisplayName) : BaseEvent;
public record UserDisplayNameChanged(Guid UserId, string DisplayName) : BaseEvent;
public record UserDeleted(Guid UserId) : BaseEvent;