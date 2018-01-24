using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace CoreBuild.Help
{
    public class Test
    {
        ITestOutputHelper output;
        MockBuildEngine engine;

        public Test(ITestOutputHelper output)
        {
            this.output = output;
            engine = new MockBuildEngine(output, true);
        }

        [Fact]
        public void Execute()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
            };

            Assert.True(task.Execute());
        }

        [Fact]
        public void HelpImportsFalse()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpImports = "false"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WRestore:")));
            Assert.True(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WBuild:")));
        }

        [Fact]
        public void HelpImportsTrue()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpImports = "true"
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WBeforeBuild:")));
        }

        [Fact]
        public void HelpPropOverride()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpImports = "false"
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WInheritedProp:")));
        }
        
        [Fact]
        public void HelpHidden()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WHiddenTarget:")));
            Assert.False(engine.LoggedMessageEvents.Any(e => e.Message.Contains("HiddenProp:")));
            Assert.False(engine.LoggedMessageEvents.Any(e => e.Message.Contains("HiddenGroup:")));
        }

        [Fact]
        public void HelpForceInclude()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpProperty = new ITaskItem[]
                {
                    new TaskItem("OnlyImportedProp"),
                },
                HelpTarget = new ITaskItem[]
                {
                    new TaskItem("OnlyImportedTarget")
                }
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => e.Message.Contains("OnlyImportedProp")));
            Assert.True(engine.LoggedMessageEvents.Any(e => e.Message.Contains("OnlyImportedTarget")));
        }

        [Fact]
        public void HelpPropsFalse()
        {

            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpProperties = "false"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => e.Message.Contains("PR:")));
        }

        [Fact]
        public void HelpPropsTrue()
        {

            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpProperties = "true"
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => e.Message.Contains("PR:")));
        }

        [Fact]
        public void HelpExcludeIncludesMetaHelp()
        {

            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpExclude = "Help"
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => e.Message.Contains("HelpProperties:")));
        }

        [Fact]
        public void HelpExcludeRemovesProperty()
        {

            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpExclude = "NuGet"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => e.Message.Contains("NuGetRestoreTargets")));
        }

        [Fact]
        public void HelpIncludeRemovesOtherProperties()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpInclude = "CI"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => e.Message.Contains("PR:")));
            Assert.True(engine.LoggedMessageEvents.Any(e => e.Message.Contains("CI:")));
        }

        [Fact]
        public void HelpNoPropertiesMatchRemovesHeader()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpInclude = "Build"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WProperties:")));
        }

        [Fact]
        public void HelpNoTargetsMatchRemovesHeader()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpInclude = "NuGet"
            };

            Assert.True(task.Execute());
            Assert.False(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WTargets:")));
        }

        [Fact]
        public void HelpSearchFiltersOut()
        {
            var task = new Help
            {
                BuildEngine = engine,
                HelpProject = Path.GetFullPath("Test.proj"),
                HelpImports = "true",
                HelpSearch = @"ProjectReferenceWithConfiguration"
            };

            Assert.True(task.Execute());
            Assert.True(engine.LoggedMessageEvents.Any(e => Regex.IsMatch(e.Message, @"\WAssignProjectConfiguration:")));
        }
    }

    #region MockBuildEngine

    public class MockBuildEngine : IBuildEngine
    {
        bool trace = false;
        ITestOutputHelper output;

        public MockBuildEngine(bool trace = true)
        {
            this.trace = trace;
            LoggedCustomEvents = new List<CustomBuildEventArgs>();
            LoggedErrorEvents = new List<BuildErrorEventArgs>();
            LoggedMessageEvents = new List<BuildMessageEventArgs>();
            LoggedWarningEvents = new List<BuildWarningEventArgs>();
        }

        public MockBuildEngine(ITestOutputHelper output, bool trace = false)
            : this(trace) => this.output = output;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            => throw new NotSupportedException();

        public int ColumnNumberOfTaskNode { get; set; }

        public bool ContinueOnError { get; set; }

        public int LineNumberOfTaskNode { get; set; }

        public string ProjectFileOfTaskNode { get; set; }

        public List<CustomBuildEventArgs> LoggedCustomEvents { get; }
        public List<BuildErrorEventArgs> LoggedErrorEvents { get; }
        public List<BuildMessageEventArgs> LoggedMessageEvents { get; }
        public List<BuildWarningEventArgs> LoggedWarningEvents { get; }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            if (trace)
                TraceMessage(e.Message);

            LoggedCustomEvents.Add(e);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            if (trace)
                TraceMessage(e.Message);

            LoggedErrorEvents.Add(e);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            if (trace)
                TraceMessage(e.Message);

            LoggedMessageEvents.Add(e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            if (trace)
                TraceMessage(e.Message);

            LoggedWarningEvents.Add(e);
        }

        void TraceMessage(string message)
        {
            Console.WriteLine(message);
            Trace.WriteLine(message);
            Debug.WriteLine(message);
            Debugger.Log(0, "", message);
            if (output != null)
                output.WriteLine(message);
        }
    }

    #endregion
}
