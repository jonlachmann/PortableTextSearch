using System.ComponentModel.DataAnnotations;

namespace PortableTextSearch.Tests.TestModel;

public sealed class MessageRecipient
{
    public int Id { get; set; }

    [MaxLength(128)]
    public string MessageId { get; set; } = null!;

    public int Type { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? Name { get; set; }
}
