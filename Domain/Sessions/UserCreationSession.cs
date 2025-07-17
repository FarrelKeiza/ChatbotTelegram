namespace Domain.Sessions;

public class UserCreationSession
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Hobby { get; set; }
    public int Step { get; set; } = 0;
}
