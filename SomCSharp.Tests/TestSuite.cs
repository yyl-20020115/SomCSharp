using Microsoft.VisualStudio.TestTools.UnitTesting;
using Som.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Universe.Main(file);
        }

    }
}
