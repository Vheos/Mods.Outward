public interface IExcludeFromBuild
{ }

public interface IWaitForPrefabs
{ }

public interface IUpdatable
{
    void OnUpdate();
    bool IsEnabled { get; }
}