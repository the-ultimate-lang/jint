﻿using Esprima;
using Ultimate.Language.Jint.Native;
using Ultimate.Language.Jint.Native.Object;
using Ultimate.Language.Jint.Native.Promise;
using Ultimate.Language.Jint.Runtime;
using Ultimate.Language.Jint.Runtime.Interpreter;
using Ultimate.Language.Jint.Runtime.Modules;

namespace Ultimate.Language.Jint
{
    public partial class Engine
    {
        internal IModuleLoader ModuleLoader { get; set; } = null!;

        private readonly Dictionary<string, ModuleRecord> _modules = new();
        private readonly Dictionary<string, ModuleBuilder> _builders = new();

        /// <summary>
        /// https://tc39.es/ecma262/#sec-getactivescriptormodule
        /// </summary>
        internal IScriptOrModule? GetActiveScriptOrModule()
        {
            return _executionContexts?.GetActiveScriptOrModule();
        }

        internal ModuleRecord LoadModule(string? referencingModuleLocation, string specifier)
        {
            var moduleResolution = ModuleLoader.Resolve(referencingModuleLocation, specifier);

            if (_modules.TryGetValue(moduleResolution.Key, out var module))
            {
                return module;
            }

            if (_builders.TryGetValue(specifier, out var moduleBuilder))
            {
                module = LoadFromBuilder(specifier, moduleBuilder, moduleResolution);
            }
            else
            {
                module = LoaderFromModuleLoader(moduleResolution);
            }

            if (module is SourceTextModuleRecord sourceTextModule)
            {
                DebugHandler.OnBeforeEvaluate(sourceTextModule._source);
            }

            return module;
        }

        private CyclicModuleRecord LoadFromBuilder(string specifier, ModuleBuilder moduleBuilder, ResolvedSpecifier moduleResolution)
        {
            var parsedModule = moduleBuilder.Parse();
            var module = new BuilderModuleRecord(this, Realm, parsedModule, null, false);
            _modules[moduleResolution.Key] = module;
            moduleBuilder.BindExportedValues(module);
            _builders.Remove(specifier);
            return module;
        }

        private CyclicModuleRecord LoaderFromModuleLoader(ResolvedSpecifier moduleResolution)
        {
            var parsedModule = ModuleLoader.LoadModule(this, moduleResolution);
            var module = new SourceTextModuleRecord(this, Realm, parsedModule, moduleResolution.Uri?.LocalPath, false);
            _modules[moduleResolution.Key] = module;
            return module;
        }

        public void AddModule(string specifier, string code)
        {
            var moduleBuilder = new ModuleBuilder(this, specifier);
            moduleBuilder.AddSource(code);
            AddModule(specifier, moduleBuilder);
        }

        public void AddModule(string specifier, Action<ModuleBuilder> buildModule)
        {
            var moduleBuilder = new ModuleBuilder(this, specifier);
            buildModule(moduleBuilder);
            AddModule(specifier, moduleBuilder);
        }

        public void AddModule(string specifier, ModuleBuilder moduleBuilder)
        {
            _builders.Add(specifier, moduleBuilder);
        }

        public ObjectInstance ImportModule(string specifier)
        {
            return ImportModule(specifier, null);
        }

        internal ObjectInstance ImportModule(string specifier, string? referencingModuleLocation)
        {
            var moduleResolution = ModuleLoader.Resolve(referencingModuleLocation, specifier);

            if (!_modules.TryGetValue(moduleResolution.Key, out var module))
            {
                module = LoadModule(null, specifier);
            }

            if (module is not CyclicModuleRecord cyclicModule)
            {
                LinkModule(specifier, module);
                EvaluateModule(specifier, module);
            }
            else if (cyclicModule.Status == ModuleStatus.Unlinked)
            {
                LinkModule(specifier, cyclicModule);

                if (cyclicModule.Status == ModuleStatus.Linked)
                {
                    ExecuteWithConstraints(true, () => EvaluateModule(specifier, cyclicModule));
                }

                if (cyclicModule.Status != ModuleStatus.Evaluated)
                {
                    ExceptionHelper.ThrowNotSupportedException($"Error while evaluating module: Module is in an invalid state: '{cyclicModule.Status}'");
                }
            }

            RunAvailableContinuations();

            return ModuleRecord.GetModuleNamespace(module);
        }

        private static void LinkModule(string specifier, ModuleRecord module)
        {
            module.Link();
        }

        private JsValue EvaluateModule(string specifier, ModuleRecord cyclicModule)
        {
            var ownsContext = _activeEvaluationContext is null;
            _activeEvaluationContext ??= new EvaluationContext(this);
            JsValue evaluationResult;
            try
            {
                evaluationResult = cyclicModule.Evaluate();
            }
            finally
            {
                if (ownsContext)
                {
                    _activeEvaluationContext = null!;
                }
            }

            // This should instead be returned and resolved in ImportModule(specifier) only so Host.ImportModuleDynamically can use this promise
            if (evaluationResult is not PromiseInstance promise)
            {
                ExceptionHelper.ThrowInvalidOperationException($"Error while evaluating module: Module evaluation did not return a promise: {evaluationResult.Type}");
            }
            else if (promise.State == PromiseState.Rejected)
            {
                var node = EsprimaExtensions.CreateLocationNode(Location.From(new Position(), new Position(), specifier));
                ExceptionHelper.ThrowJavaScriptException(this, promise.Value, node.Location);
            }
            else if (promise.State != PromiseState.Fulfilled)
            {
                ExceptionHelper.ThrowInvalidOperationException($"Error while evaluating module: Module evaluation did not return a fulfilled promise: {promise.State}");
            }

            return evaluationResult;
        }
    }
}
