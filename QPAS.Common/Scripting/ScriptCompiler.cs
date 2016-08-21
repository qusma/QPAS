// -----------------------------------------------------------------------
// <copyright file="ScriptCompiler.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using EntityModel;
using Microsoft.CSharp;

namespace QPAS.Scripting
{
    public static class ScriptCompiler
    {
        public static void CompileScriptsToDLL(IEnumerable<UserScript> scripts)
        {
            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = false,
                TreatWarningsAsErrors = false,
                GenerateExecutable = false,
                CompilerOptions = "/optimize",
                OutputAssembly = "UserScripts.dll"
            };

            compilerParams.ReferencedAssemblies.AddRange(scripts.SelectMany(x => x.ReferencedAssemblies).Distinct().ToArray());

            var provider = new CSharpCodeProvider();

            CompilerResults compile = provider.CompileAssemblyFromSource(compilerParams, scripts.Select(x => x.Code).ToArray());

            if (compile.Errors.HasErrors)
            {
                string text = compile.Errors.Cast<CompilerError>().Aggregate("Compile error:\n", (current, ce) => current + ce + "\n");
                throw new Exception(text);
            }
        }
    }
}
