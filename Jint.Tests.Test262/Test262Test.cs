﻿using Esprima;
using Jint.Native;
using Jint.Native.ArrayBuffer;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Test262Harness;

namespace Jint.Tests.Test262;

public abstract partial class Test262Test
{
    private Engine BuildTestExecutor(Test262File file)
    {
        var engine = new Engine(cfg =>
        {
            var relativePath = Path.GetDirectoryName(file.FileName);
            cfg.EnableModules(new Test262ModuleLoader(State.Test262Stream.Options.FileSystem, relativePath));
        });

        if (file.Flags.IndexOf("raw") != -1)
        {
            // nothing should be loaded
            return engine;
        }

        engine.Execute(State.Sources["assert.js"]);
        engine.Execute(State.Sources["sta.js"]);

        engine.SetValue("print", new ClrFunctionInstance(engine, "print", (_, args) => TypeConverter.ToString(args.At(0))));

        var o = engine.Realm.Intrinsics.Object.Construct(Arguments.Empty);
        o.FastSetProperty("evalScript", new DataPropertyDescriptor(new ClrFunctionInstance(engine, "evalScript",
            (_, args) =>
            {
                if (args.Length > 1)
                {
                    throw new Exception("only script parsing supported");
                }

                var options = new ParserOptions { RegExpParseMode = RegExpParseMode.AdaptToInterpreted, Tolerant = false };
                var parser = new JavaScriptParser(options);
                var script = parser.ParseScript(args.At(0).AsString());

                return engine.Evaluate(script);
            }), true, true, true));

        o.FastSetProperty("createRealm", new DataPropertyDescriptor(new ClrFunctionInstance(engine, "createRealm",
            (_, args) =>
            {
                var realm = engine._host.CreateRealm();
                realm.GlobalObject.Set("global", realm.GlobalObject);
                return realm.GlobalObject;
            }), true, true, true));

        o.FastSetProperty("detachArrayBuffer", new DataPropertyDescriptor(new ClrFunctionInstance(engine, "detachArrayBuffer",
            (_, args) =>
            {
                var buffer = (JsArrayBuffer) args.At(0);
                buffer.DetachArrayBuffer();
                return JsValue.Undefined;
            }), true, true, true));

        o.FastSetProperty("gc", new DataPropertyDescriptor(new ClrFunctionInstance(engine, "gc",
            (_, _) =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return JsValue.Undefined;
            }), true, true, true));

        engine.SetValue("$262", o);

        foreach (var include in file.Includes)
        {
            engine.Execute(State.Sources[include]);
        }

        if (file.Flags.IndexOf("async") != -1)
        {
            engine.Execute(State.Sources["doneprintHandle.js"]);
        }

        return engine;
    }

    private static void ExecuteTest(Engine engine, Test262File file)
    {
        if (file.Type == ProgramType.Module)
        {
            var specifier = "./" + Path.GetFileName(file.FileName);
            engine.AddModule(specifier, builder => builder.AddSource(file.Program));
            engine.ImportModule(specifier);
        }
        else
        {
            engine.Execute(new JavaScriptParser().ParseScript(file.Program, source: file.FileName));
        }
    }

    private partial bool ShouldThrow(Test262File testCase, bool strict)
    {
        return testCase.Negative;
    }
}
