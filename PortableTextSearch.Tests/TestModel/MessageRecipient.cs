namespace PortableTextSearch.Tests.TestModel;

public sealed class MessageRecipient
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public int Type { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }
}
