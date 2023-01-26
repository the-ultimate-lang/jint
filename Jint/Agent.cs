using Ultimate.Language.Jint.Native;

namespace Ultimate.Language.Jint;

/// <summary>
/// https://tc39.es/ecma262/#sec-agents , still a work in progress, mostly placeholder
/// </summary>
internal sealed class Agent
{
    private List<JsValue> _keptAlive = new();

    public void AddToKeptObjects(JsValue target)
    {
        _keptAlive.Add(target);
    }

    public void ClearKeptObjects()
    {
        _keptAlive.Clear();
    }
}
