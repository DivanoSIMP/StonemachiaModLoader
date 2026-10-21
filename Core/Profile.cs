namespace StonemachiaModLoader.Core;

public sealed class Profile
{
    public Profile(string name)
    {
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Profile name cannot be empty.", nameof(name));
    }

    public string Name { get; }

    public string GetName() => Name;

    public override string ToString() => Name;
}
