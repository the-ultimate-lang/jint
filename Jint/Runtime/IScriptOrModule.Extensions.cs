using Esprima;
using Ultimate.Language.Jint.Runtime.Modules;

namespace Ultimate.Language.Jint.Runtime;

internal static class ScriptOrModuleExtensions
{
    public static ModuleRecord AsModule(this IScriptOrModule? scriptOrModule, Engine engine, Location location)
    {
        var module = scriptOrModule as ModuleRecord;
        if (module == null)
        {
            ExceptionHelper.ThrowSyntaxError(engine.Realm, "Cannot use import/export statements outside a module", location);
            return default!;
        }
        return module;
    }
}
