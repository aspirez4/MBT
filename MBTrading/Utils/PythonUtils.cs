using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MBTrading.Utils
{
    public class PythonUtils
    {
        private ScriptEngine engine;
        private ScriptScope scope;
        private ScriptSource source;
        private CompiledCode compiled;
        private object pythonClass;

        public PythonUtils(string codePath, string className = "PyClass")
        {
            var lang = Python.CreateLanguageSetup(null); 
            lang.Options["Frames"] = ScriptingRuntimeHelpers.True;
            var setup = new ScriptRuntimeSetup(); 
            setup.LanguageSetups.Add(lang); 
            var runtime = new ScriptRuntime(setup); 
            var engine = runtime.GetEngine("py"); 
            engine.ExecuteFile(codePath);



            string code = File.ReadAllText(codePath);
            
            //creating engine and stuff
            engine = Python.CreateEngine();
            var sp = engine.GetSearchPaths();
            sp.Add(@"c:\Program Files\IronPython 2.7");
            sp.Add(@"c:\Program Files\IronPython 2.7\DLLs");
            sp.Add(@"c:\Program Files\IronPython 2.7\Lib");
            sp.Add(@"c:\Program Files\IronPython 2.7\Lib\site-packages");
            sp.Add(@"c:\Users\Or\AppData\Local\Enthought\Canopy\User\Scripts");

            engine.SetSearchPaths(sp);

            scope = engine.CreateScope();

            //loading and compiling code
            source = engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
            compiled = source.Compile();


            var _runtime = engine.Runtime;
            var scope1 = _runtime.ExecuteFile(codePath);


            //now executing this code (the code should contain a class)
            compiled.Execute(scope);

            //now creating an object that could be used to access the stuff inside a python script
            pythonClass = engine.Operations.Invoke(scope.GetVariable(className));
        }

        public void SetVariable(string variable, dynamic value)
        {
            scope.SetVariable(variable, value);
        }

        public dynamic GetVariable(string variable)
        {
            return scope.GetVariable(variable);
        }

        public void CallMethod(string method, params dynamic[] arguments)
        {
            engine.Operations.InvokeMember(pythonClass, method, arguments);
        }

        public dynamic CallFunction(string method, params dynamic[] arguments)
        {
            return engine.Operations.InvokeMember(pythonClass, method, arguments);
        }

    }
}
