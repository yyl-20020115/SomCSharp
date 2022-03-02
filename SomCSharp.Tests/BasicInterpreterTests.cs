using Som.VM;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SomCSharp.Tests;

[TestClass]
public class BasicInterpreterTests
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
        var folder = Path.Combine(current,"core-lib","TestSuite",this.GetType().Name);
        var files = Directory.GetFiles(folder,"*.som");
        foreach(var file in files)
        {
            Universe.Main(file);
        }
    }
}
