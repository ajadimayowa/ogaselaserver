namespace Ogasela.Domain.Geo;

public class NigeriaState
{
    private NigeriaState()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Short display code, e.g. "LA" for Lagos. Not necessarily a real ISO subdivision code - just a convenient short label.</summary>
    public string Code { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static NigeriaState Create(string name, string code)
    {
        return new NigeriaState
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string code)
    {
        Name = name;
        Code = code;
    }
}
