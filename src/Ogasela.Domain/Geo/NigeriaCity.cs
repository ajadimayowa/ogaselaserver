namespace Ogasela.Domain.Geo;

public class NigeriaCity
{
    private NigeriaCity()
    {
    }

    public Guid Id { get; private set; }

    public Guid StateId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static NigeriaCity Create(Guid stateId, string name)
    {
        return new NigeriaCity
        {
            Id = Guid.NewGuid(),
            StateId = stateId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
