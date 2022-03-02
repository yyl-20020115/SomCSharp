using Som.VM;
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Som.VMObject;
using Som.Compiler;

namespace SomCSharp.Tests;

[TestClass]
public class BasicInterpreterTests
{
    public static object[][] data = new object[][]
    {
        new object[]{"MethodCall", "test", 42, typeof(SInteger)},
        new object[]{"MethodCall", "test2", 42, typeof(SInteger)},
        new object[]{ "NonLocalReturn", "test1", 42, typeof(SInteger)},
        new object[]{ "NonLocalReturn", "test2", 43, typeof(SInteger)},
        new object[]{ "NonLocalReturn", "test3", 3, typeof(SInteger)},
        new object[]{ "NonLocalReturn", "test4", 42, typeof(SInteger)},
        new object[]{ "NonLocalReturn", "test5", 22, typeof(SInteger)},
        new object[]{ "Blocks", "testArg1", 42, typeof(SInteger)},
        new object[]{ "Blocks", "testArg2", 77, typeof(SInteger)},
        new object[]{ "Blocks", "testArgAndLocal", 8, typeof(SInteger)},
        new object[]{ "Blocks", "testArgAndContext", 8, typeof(SInteger)},
        new object[]{ "Blocks", "testEmptyZeroArg", 1, typeof(SInteger)},
        new object[]{ "Blocks", "testEmptyOneArg", 1, typeof(SInteger)},
        new object[]{ "Blocks", "testEmptyTwoArg", 1, typeof(SInteger)},
        new object[]{ "Return", "testReturnSelf", "Return", typeof(SClass)},
        new object[]{ "Return", "testReturnSelfImplicitly", "Return", typeof(SClass)},
        new object[]{ "Return", "testNoReturnReturnsSelf", "Return", typeof(SClass)},
        new object[]{ "Return", "testBlockReturnsImplicitlyLastValue", 4, typeof(SInteger)},
        new object[]{ "IfTrueIfFalse", "test", 42, typeof(SInteger)},
        new object[]{ "IfTrueIfFalse", "test2", 33, typeof(SInteger)},
        new object[]{ "IfTrueIfFalse", "test3", 4, typeof(SInteger)},
        new object[]{ "IfTrueIfFalse", "testIfTrueTrueResult", "Integer", typeof(SClass)},
        new object[]{ "IfTrueIfFalse", "testIfTrueFalseResult", "Nil", typeof(SClass)},
        new object[]{ "IfTrueIfFalse", "testIfFalseTrueResult", "Nil", typeof(SClass)},
        new object[]{ "IfTrueIfFalse", "testIfFalseFalseResult", "Integer", typeof(SClass)},
    
        new object[]{ "CompilerSimplification", "testReturnConstantSymbol", "constant", typeof(SSymbol)},
        new object[]{ "CompilerSimplification", "testReturnConstantInt", 42, typeof(SInteger)},
        new object[]{ "CompilerSimplification", "testReturnSelf", "CompilerSimplification", typeof(SClass)},
        new object[]{
            "CompilerSimplification", "testReturnSelfImplicitly", "CompilerSimplification",
                    typeof(SClass)},
        new object[]{ "CompilerSimplification", "testReturnArgumentN", 55, typeof(SInteger)},
        new object[]{ "CompilerSimplification", "testReturnArgumentA", 44, typeof(SInteger)},
        new object[]{ "CompilerSimplification", "testSetField", "foo", typeof(SSymbol)},
        new object[]{ "CompilerSimplification", "testGetField", 40, typeof(SInteger)},

        new object[]{ "Hash", "testHash", 444, typeof(SInteger)},

        new object[]{ "Arrays", "testEmptyToInts", 3, typeof(SInteger)},
        new object[]{ "Arrays", "testPutAllInt", 5, typeof(SInteger)},
        new object[]{ "Arrays", "testPutAllNil", "Nil", typeof(SClass)},
        new object[]{ "Arrays", "testPutAllBlock", 3, typeof(SInteger)},
        new object[]{ "Arrays", "testNewWithAll", 1, typeof(SInteger)},

        new object[]{ "BlockInlining", "testNoInlining", 1, typeof(SInteger)},
        new object[]{ "BlockInlining", "testOneLevelInlining", 1, typeof(SInteger)},
        new object[]{ "BlockInlining", "testOneLevelInliningWithLocalShadowTrue", 2, typeof(SInteger)},
        new object[]{ "BlockInlining", "testOneLevelInliningWithLocalShadowFalse", 1, typeof(SInteger)},

        new object[]{ "BlockInlining", "testShadowDoesntStoreWrongLocal", 33, typeof(SInteger)},
        new object[]{ "BlockInlining", "testShadowDoesntReadUnrelated", "Nil", typeof(SClass)},

        new object[]{ "BlockInlining", "testBlockNestedInIfTrue", 2, typeof(SInteger)},
        new object[]{ "BlockInlining", "testBlockNestedInIfFalse", 42, typeof(SInteger)},

        new object[]{ "BlockInlining", "testStackDisciplineTrue", 1, typeof(SInteger)},
        new object[]{ "BlockInlining", "testStackDisciplineFalse", 2, typeof(SInteger)},

        new object[]{ "BlockInlining", "testDeepNestedInlinedIfTrue", 3, typeof(SInteger)},
        new object[]{ "BlockInlining", "testDeepNestedInlinedIfFalse", 42, typeof(SInteger)},

        new object[]{ "BlockInlining", "testDeepNestedBlocksInInlinedIfTrue", 5, typeof(SInteger)},
        new object[]{ "BlockInlining", "testDeepNestedBlocksInInlinedIfFalse", 43, typeof(SInteger)},

        new object[]{ "BlockInlining", "testDeepDeepNestedTrue", 9, typeof(SInteger)},
        new object[]{ "BlockInlining", "testDeepDeepNestedFalse", 43, typeof(SInteger)},

        new object[]{ "BlockInlining", "testToDoNestDoNestIfTrue", 2, typeof(SInteger)},

        new object[]{ "NonLocalVars", "testWriteDifferentTypes", 3.75, typeof(SDouble)},

        new object[]{ "ObjectCreation", "test", 1000000, typeof(SInteger)},

        new object[]{ "Regressions", "testSymbolEquality", 1, typeof(SInteger)},
        new object[]{ "Regressions", "testSymbolReferenceEquality", 1, typeof(SInteger)},
        new object[]{ "Regressions", "testUninitializedLocal", 1, typeof(SInteger)},
        new object[]{ "Regressions", "testUninitializedLocalInBlock", 1, typeof(SInteger)},

        new object[]{ "BinaryOperation", "test", 3 + 8, typeof(SInteger)},

        new object[]{ "NumberOfTests", "numberOfTests", 65, typeof(SInteger)}
    };
    protected void assertExpectedEqualsSOMValue(object actualResult, object value, Type type)
    {
        if (type == typeof(SInteger) && actualResult is SInteger s)
        {
            Assert.AreEqual(s.EmbeddedInteger, (long)(int)value);
        }
        else if (type == typeof(SDouble) && actualResult is SDouble d)
        {
            Assert.AreEqual(d.EmbeddedDouble, value);
        }
        else if (type == typeof(SClass) && actualResult is SClass c)
        {
            Assert.AreEqual(c.Name.EmbeddedString, value);
        }
        else if (type == typeof(SSymbol) && actualResult is SSymbol b)
        {
            Assert.AreEqual(b.EmbeddedString, value);
        }
        else
        {
            Assert.Fail("SOM Value handler missing for " + type);
        }
    }

    public void TestCore(string folder,string testClass, string testSelector,object value, Type type)
    {
        Universe u = new Universe(true);
        u.SetupClassPath(folder);

        try
        {
            var actualResult = u.Interpret(
                testClass,
                testSelector);
            assertExpectedEqualsSOMValue(actualResult,value,type);
        }
        catch (ProgramDefinitionError e)
        {
            Assert.Fail(e.Message);
        }
    }

    [TestMethod]
    public void PerformTest()
    {
        var current = Environment.CurrentDirectory;
        var test_folder = current;
        var smalltalk_folder = "Smalltalk";
        if (current.EndsWith("net6.0", StringComparison.OrdinalIgnoreCase))
        {
            current = Environment.CurrentDirectory =
                new DirectoryInfo(
                    current + "\\..\\..\\..\\..\\SomCSharp\\").FullName;
            test_folder = Path.Combine(current, "core-lib\\TestSuite\\BasicInterpreterTests");
        }
        test_folder = smalltalk_folder + Path.PathSeparator + test_folder;

        foreach (var t in data)
        {
            this.TestCore(test_folder, t[0] as string, t[1] as string, t[2], t[3] as Type);
        }
    }
}
