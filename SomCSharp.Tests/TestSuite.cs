using Som.VM;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SomCSharp.Tests;

[TestClass]
public class TestSuite
{
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

            Universe.Main(args);
        }

    }
}
