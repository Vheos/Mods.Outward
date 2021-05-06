public interface IDevelopmentOnly
{ }

public interface IDelayedInit
{ }

public interface IUpdatable
{
    void OnUpdate();
    bool IsEnabled { get; }
}