using Som.VM;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Som.Compiler;

namespace SomCSharp.Tests;

[TestClass]
public class TestSuite
{
    protected int TestCore(params string[] arguments)
    {
        var u = new Universe(true);

        // Start interpretation
        try
        {
            u.Interpret(arguments);
        }
        catch (ProgramDefinitionError e)
        {
            u.ErrorExit(e.ToString());
        }

        // Exit with error code 0
        return u.Exit(0);

    }
    [TestMethod]
    public void PerformTest()
    {
        var current = Environment.CurrentDirectory;
        if (current.EndsWith("net6.0", StringComparison.OrdinalIgnoreCase))
        {
            current = Environment.CurrentDirectory =
                new DirectoryInfo(current + "\\..\\..\\..\\..\\SomCSharp\\").FullName;
        }
        var folder = Path.Combine(current, "core-lib", "TestSuite");

        var files = Directory.GetFiles(folder, "*.som");
        foreach (var file in files)
        {
            var info = new FileInfo(file);

            string[] args = { "-cp", "Smalltalk", "TestSuite/TestHarness.som", info.Name };

            Assert.IsTrue(0== this.TestCore(args));
        }

    }
}
