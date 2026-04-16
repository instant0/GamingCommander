namespace GamingCommander.Core;

public interface ILauncher
{
    string Name { get; }
    bool IsAvailable { get; }
    IReadOnlyList<IGame> Detect();
    void Launch(IGame game);
}
