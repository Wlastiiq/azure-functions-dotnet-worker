﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.Azure.Functions.Worker.Core;
using System.Reflection;
using System.Collections.Generic;

namespace Sdk.Generator.Tests
{
    static class TestHelpers
    {
        public static Task RunTestAsync<TSourceGenerator>(
            IEnumerable<Assembly> extensionAssemblyReferences,
            string inputSource,
            string expectedFileName,
            string expectedOutputSource) where TSourceGenerator : ISourceGenerator, new()
        {
            CSharpSourceGeneratorVerifier<TSourceGenerator>.Test test = new()
            {
                TestState =
            {
                Sources = { inputSource },
                GeneratedSources =
                {
                    (typeof(TSourceGenerator), expectedFileName, SourceText.From(expectedOutputSource, Encoding.UTF8, SourceHashAlgorithm.Sha1)),
                },
                AdditionalReferences =
                {
                    typeof(WorkerExtensionStartupAttribute).Assembly,
                },
            },
            };

            foreach (var item in extensionAssemblyReferences)
            {
                test.TestState.AdditionalReferences.Add(item);
            }

            return test.RunAsync();
        }

    }

    // Mostly copy/pasted from the Microsoft Source Generators testing documentation
    public static class CSharpSourceGeneratorVerifier<TSourceGenerator> where TSourceGenerator : ISourceGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
        {
            public Test()
            {
                // See https://www.nuget.org/packages/Microsoft.NETCore.App.Ref/6.0.0
                this.ReferenceAssemblies = new ReferenceAssemblies(
                    targetFramework: "net6.0",
                    referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"),
                    referenceAssemblyPath: Path.Combine("ref", "net6.0"));
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp9;

            protected override CompilationOptions CreateCompilationOptions()
            {
                CompilationOptions compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                     compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
            }

            static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
            {
                string[] args = { "/warnaserror:nullable" };
                var commandLineArguments = CSharpCommandLineParser.Default.Parse(
                    args,
                    baseDirectory: Environment.CurrentDirectory,
                    sdkDirectory: Environment.CurrentDirectory);
                var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

                return nullableWarnings;
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(this.LanguageVersion);
            }
        }
    }
}