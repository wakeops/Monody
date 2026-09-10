namespace Monody.Data.Entities;

public class UserMemory
{
    public int Id { get; set; }

    public ulong UserId { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    public string Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
