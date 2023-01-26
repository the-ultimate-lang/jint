using Esprima.Ast;

namespace Ultimate.Language.Jint.Runtime;

internal sealed record ScriptRecord(Realm Realm, Script EcmaScriptCode, string Location) : IScriptOrModule;
