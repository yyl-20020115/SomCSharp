/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
 * Copyright (c) 2016 Michael Haupt, github@haupz.de
 * Copyright (c) 2009 Michael Haupt, michael.haupt@hpi.uni-potsdam.de
 * Software Architecture Group, Hasso Plattner Institute, Potsdam, Germany
 * http://www.hpi.uni-potsdam.de/swa/
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

namespace Som.VM;
using Som.Compiler;
using Som.Interpreter;
using Som.VMObject;
using System.Numerics;
using static Som.Interpreter.Bytecodes;

public class Universe
{
    public static void Main(string[] arguments)
    {
        // Create Universe
        var u = new Universe();

        // Start interpretation
        try
        {
            u.interpret(arguments);
        }
        catch (ProgramDefinitionError e)
        {
            u.errorExit(e.ToString());
        }

        // Exit with error code 0
        u.exit(0);
    }

    public SAbstractObject interpret(string[] arguments)
    {
        // Check for command line switches
        arguments = handleArguments(arguments);

        // Initialize the known universe
        return initialize(arguments);
    }

    static Universe()
    { /* static initializer */
        pathSeparator = Path.DirectorySeparatorChar.ToString();
        fileSeparator = Path.DirectorySeparatorChar.ToString();
    }

    public Universe()
    {
        this.interpreter = new (this);
        this.symbolTable = new ();
        this.avoidExit = false;
        this.lastExitCode = 0;

        current = this;
    }

    public Universe(bool avoidExit)
    {
        this.interpreter = new (this);
        this.symbolTable = new ();
        this.avoidExit = avoidExit;
        this.lastExitCode = 0;

        current = this;
    }

    public static Universe Current() => current;

    public Interpreter getInterpreter() => interpreter;

    public void exit(long errorCode)
    {
        // Exit from the Java system
        if (!avoidExit)
        {
            Environment.Exit((int)errorCode);
        }
        else
        {
            lastExitCode = (int)errorCode;
        }
    }

    public int LastExitCode() => lastExitCode;

    public void errorExit(string message)
    {
        errorPrintln("Runtime Error: " + message);
        exit(1);
    }

    private string[] handleArguments(string[] arguments)
    {
        bool gotClasspath = false;
        List<string> remainingArgs = new();

        // read dash arguments only while we haven't seen other kind of arguments
        bool sawOthers = false;

        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == ("-cp") && !sawOthers)
            {
                if (i + 1 >= arguments.Length)
                {
                    printUsageAndExit();
                }
                setupClassPath(arguments[i + 1]);
                // Checkstyle: stop
                ++i; // skip class path
                     // Checkstyle: resume
                gotClasspath = true;
            }
            else if (arguments[i] == ("-d") && !sawOthers)
            {
                dumpBytecodes = true;
            }
            else
            {
                sawOthers = true;
                remainingArgs.Add(arguments[i]);
            }
        }

        if (!gotClasspath)
        {
            // Get the default class path of the appropriate size
            classPaths = setupDefaultClassPath(0);
        }

        // check first of remaining args for class paths, and strip file extension

        if (remainingArgs.Count > 0)
        {
            var split = getPathClassExt(remainingArgs[0]);

            if (!string.IsNullOrEmpty(split[0]))
            { // there was a path
                classPaths.Insert(0, split[0]);
            }
            remainingArgs[0]=split[1];
        }

        return remainingArgs.ToArray();
    }

    // take argument of the form "../foo/Test.som" and return
    // "../foo", "Test", "som"
    private string[] getPathClassExt(string arg) => Array.Empty<string>();

    public void setupClassPath(string cp)
    {
    }

    private List<string> setupDefaultClassPath(int directories)
    {
        // Get the default system class path
        var systemClassPath = Environment.CurrentDirectory;

        // Compute the number of defaults
        int defaults = (systemClassPath != null) ? 2 : 1;

        // Allocate an array with room for the directories and the defaults
        var result = new List<string>();

        // Insert the system class path into the defaults section
        if (systemClassPath != null)
        {
            result.Add(Path.Combine(systemClassPath, "."));

            result.Add(Path.Combine(systemClassPath, "core-lib\\Smalltalk"));
        }

        result.Add(".");

        // Return the class path
        return result;
    }

    private void printUsageAndExit()
    {
        // Print the usage
        println("Usage: som [-options] [args...]                          ");
        println("                                                         ");
        println("where options include:                                   ");
        println("    -cp <directories separated by " + pathSeparator  + ">");
        println("                  set search path for application classes");
        println("    -d            enable disassembling");
        // Exit
        Environment.Exit(0);
    }

    /**
     * Start interpretation by sending the selector to the given class.
     * This is mostly meant for testing currently.
     *
     * @param className
     * @param selector
     * @return
     * @throws ProgramDefinitionError
     */
    public SAbstractObject interpret(string className,string selector)
    {
        initializeObjectSystem();

        var clazz = loadClass(symbolFor(className));

        // Lookup the initialize invokable on the system class
        var initialize =
            (SMethod)clazz.getSOMClass(this).lookupInvokable(symbolFor(selector));

        if (initialize == null) {
            throw new Exception("Lookup of " + className + ">>#" + selector + " failed");
        }

        return interpretMethod(clazz, initialize, null);
    }

    private SAbstractObject initialize(string[] arguments)
    {
        var systemObject = initializeObjectSystem();
        // Start the shell if no filename is given
        if (arguments.Length == 0) {
            var shell = new Shell(this, interpreter);
            var bootstrapMethod = createBootstrapMethod();
            shell.setBootstrapMethod(bootstrapMethod);
            return shell.start();
        }

        // Lookup the initialize invokable on the system class
        var initialize = systemClass.lookupInvokable(symbolFor("initialize:"));
        // Convert the arguments into an array
        var argumentsArray = newArray(arguments);

        return interpretMethod(systemObject, initialize,argumentsArray);
    }

    private SMethod createBootstrapMethod()
    {
        // Create a fake bootstrap method to simplify later frame traversal
        var bootstrapMethod = newMethod(symbolFor("bootstrap"), 1, 0, 2, null);
        bootstrapMethod.setBytecode(0, HALT);
        bootstrapMethod.setHolder(systemClass);
        return bootstrapMethod;
    }

    private SAbstractObject interpretMethod(SAbstractObject receiver, SInvokable invokable, SArray arguments)
    {
        var bootstrapMethod = createBootstrapMethod();
        // Create a fake bootstrap frame with the system object on the stack
        var bootstrapFrame = interpreter.pushNewFrame(bootstrapMethod);
        bootstrapFrame.push(receiver);

        if (arguments != null) {
            bootstrapFrame.push(arguments);
        }

        // Invoke the initialize invokable
        invokable.invoke(bootstrapFrame, interpreter);
        // Start the interpreter
        return interpreter.start();
    }

    private SAbstractObject initializeObjectSystem()
    {
        // Allocate the nil object
        this.nilObject = new SObject(null);
        // Allocate the Metaclass classes
        this.metaclassClass = this.newMetaclassClass();

        // Allocate the rest of the system classes
        this.objectClass = this.newSystemClass();
        this.nilClass = this.newSystemClass();
        this.classClass = this.newSystemClass();
        this.arrayClass = this.newSystemClass();
        this.symbolClass = this.newSystemClass();
        this.methodClass = this.newSystemClass();
        this.integerClass = this.newSystemClass();
        this.primitiveClass = this.newSystemClass();
        this.stringClass = this.newSystemClass();
        this.doubleClass = this.newSystemClass();

        // Setup the class reference for the nil object
        this.nilObject.setClass(this.nilClass);

        // Initialize the system classes.
        this.initializeSystemClass(this.objectClass, null, "Object");
        this.initializeSystemClass(this.classClass, this.objectClass, "Class");
        this.initializeSystemClass(this.metaclassClass, this.classClass, "Metaclass");
        this.initializeSystemClass(this.nilClass, this.objectClass, "Nil");
        this.initializeSystemClass(this.arrayClass, this.objectClass, "Array");
        this.initializeSystemClass(this.methodClass, this.arrayClass, "Method");
        this.initializeSystemClass(this.stringClass, this.objectClass, "String");
        this.initializeSystemClass(this.symbolClass, this.stringClass, "Symbol");
        this.initializeSystemClass(this.integerClass, this.objectClass, "Integer");
        this.initializeSystemClass(this.primitiveClass, this.objectClass, "Primitive");
        this.initializeSystemClass(this.doubleClass, this.objectClass, "Double");

        // Load methods and fields into the system classes
        this.loadSystemClass(this.objectClass);
        this.loadSystemClass(this.classClass);
        this.loadSystemClass(this.metaclassClass);
        this.loadSystemClass(this.nilClass);
        this.loadSystemClass(this.arrayClass);
        this.loadSystemClass(this.methodClass);
        this.loadSystemClass(this.symbolClass);
        this.loadSystemClass(this.integerClass);
        this.loadSystemClass(this.primitiveClass);
        this.loadSystemClass(this.stringClass);
        this.loadSystemClass(this.doubleClass);

        // Fix up objectClass
        this.objectClass.setSuperClass(this.nilObject);

        // Load the generic block class
        this.blockClass = this.loadClass(symbolFor("Block"));

        // Setup the true and false objects
        var trueSymbol = this.symbolFor("True");
        this.trueClass = this.loadClass(trueSymbol);
        this.trueObject = this.newInstance(trueClass);

        var falseSymbol = this.symbolFor("False");
        this.falseClass = this.loadClass(falseSymbol);
        this.falseObject = this.newInstance(this.falseClass);

        // Load the system class and create an instance of it
        this.systemClass = this.loadClass(this.symbolFor("System"));
        var systemObject = this.newInstance(this.systemClass);

        // Put special objects and classes into the dictionary of globals
        this.setGlobal(this.symbolFor("nil"), this.nilObject);
        this.setGlobal(this.symbolFor("true"), this.trueObject);
        this.setGlobal(this.symbolFor("false"), this.falseObject);
        this.setGlobal(this.symbolFor("system"), systemObject);
        this.setGlobal(this.symbolFor("System"), this.systemClass);
        this.setGlobal(this.symbolFor("Block"), this.blockClass);

        this.setGlobal(trueSymbol, this.trueClass);
        this.setGlobal(falseSymbol, this.falseClass);
        return systemObject;
    }

    public SSymbol symbolFor(string text) =>
        // Lookup the symbol in the symbol table
        this.symbolTable.TryGetValue(text, out var s) ? s : newSymbol(text);

    public SArray newArray(long length) => new (nilObject, length);

    public SArray newArray<T>(List<T> list) where T : class
    {
        // Allocate a new array with the same length as the list
        var result = newArray(list.Count);
        // Copy all elements from the list into the array
        for (int i = 0; i < list.Count; i++)
        {
            result.setIndexableField(i, list[i] as SAbstractObject);
        }
        // Return the allocated and initialized array
        return result;
    }

    public SArray newArray(string[] stringArray)
    {
        // Allocate a new array with the same length as the string array
        var result = newArray(stringArray.Length);
        // Copy all elements from the string array into the array
        for (int i = 0; i < stringArray.Length; i++)
        {
            result.setIndexableField(i, newString(stringArray[i]));
        }

        // Return the allocated and initialized array
        return result;
    }

    public SBlock newBlock(SMethod method, Frame context, int arguments) =>
        // Allocate a new block and set its class to be the block class
        new SBlock(method, context, getBlockClass(arguments));

    public SClass newClass(SClass classClass)
    {
        // Allocate a new class and set its class to be the given class class
        var result = new SClass(classClass.getNumberOfInstanceFields(), this);
        result.setClass(classClass);
        // Return the freshly allocated class
        return result;
    }

    public Frame newFrame(Frame previousFrame, SMethod method, Frame context)
    {
        // Compute the maximum number of stack locations (including arguments,
        // locals and extra buffer to support doesNotUnderstand) and set the number
        // of indexable fields accordingly
        var length = method.getNumberOfArguments()
            + method.getNumberOfLocals()
            + method.getMaximumNumberOfStackElements() + 2;

        return new Frame(nilObject, previousFrame, context, method, length);
    }

    public SMethod newMethod(SSymbol signature, int numberOfBytecodes,
         int numberOfLocals,
         int maxNumStackElements, List<SAbstractObject> literals) =>
        // Allocate a new method and set its class to be the method class
        new SMethod(signature, numberOfBytecodes,
            numberOfLocals, maxNumStackElements, literals);

    public SObject newInstance(SClass instanceClass)
    {
        // Allocate a new instance and set its class to be the given class
        var result = new SObject(instanceClass.getNumberOfInstanceFields(),
            nilObject);
        result.setClass(instanceClass);
        // Return the freshly allocated instance
        return result;
    }

    public SInteger newInteger(long value) => SInteger.getInteger(value);

    public SBigInteger newBigInteger(BigInteger value) => new (value);

    public SDouble newDouble(double value) => new (value);

    public SClass newMetaclassClass()
    {
        // Allocate the metaclass classes
        var result = new SClass(this);
        result.setClass(new SClass(this));
        // Setup the metaclass hierarchy
        result.getSOMClass().setClass(result);
        // Return the freshly allocated metaclass class
        return result;
    }

    public SString newString(string embeddedString) =>
        // Allocate a new string and set its class to be the string class
        new (embeddedString);

    private SSymbol newSymbol(string str) =>
        // Allocate a new symbol and set its class to be the symbol class
        // Insert the new symbol into the symbol table
        this.symbolTable[str] = new (str);// Return the freshly allocated symbol

    public SClass newSystemClass()
    {
        // Allocate the new system class
        var systemClass = new SClass(this);
        // Setup the metaclass hierarchy
        systemClass.setClass(new SClass(this));
        systemClass.getSOMClass().setClass(metaclassClass);
        // Return the freshly allocated system class
        return systemClass;
    }

    public void initializeSystemClass(SClass systemClass, SClass superClass,string name)
    {
        // Initialize the superclass hierarchy
        if (superClass != null)
        {
            systemClass.setSuperClass(superClass);
            systemClass.getSOMClass().setSuperClass(superClass.getSOMClass());
        }
        else
        {
            systemClass.getSOMClass().setSuperClass(classClass);
        }

        // Initialize the array of instance fields
        systemClass.setInstanceFields(newArray(0));
        systemClass.getSOMClass().setInstanceFields(newArray(0));
        // Initialize the array of instance invokables
        systemClass.setInstanceInvokables(newArray(0));
        systemClass.getSOMClass().setInstanceInvokables(newArray(0));
        // Initialize the name of the system class
        systemClass.setName(symbolFor(name));
        systemClass.getSOMClass().setName(symbolFor(name + " class"));
        // Insert the system class into the dictionary of globals
        this.setGlobal(systemClass.getName(), systemClass);
    }

    public SAbstractObject getGlobal(SSymbol name) =>
        // Return the global with the given name if it's in the dictionary of
        // globals
        this.hasGlobal(name) ? globals[name] : null;

    public void setGlobal(SSymbol name, SAbstractObject value) =>
        // Insert the given value into the dictionary of globals
        this.globals[name] = value;

    public bool hasGlobal(SSymbol name) =>
        // Returns if the universe has a value for the global of the given name
        this.globals.ContainsKey(name);

    public SClass getBlockClass() =>
        // Get the generic block class
        this.blockClass;

    public SClass getBlockClass(int numberOfArguments)
    {
        // Compute the name of the block class with the given number of
        // arguments
        var name = symbolFor("Block" + (numberOfArguments));
        // Lookup the specific block class in the dictionary of globals and
        // return it
        if (this.hasGlobal(name)) return (SClass)this.getGlobal(name);
        // Get the block class for blocks with the given number of arguments
        var result = loadClass(name, null);
        // Add the appropriate value primitive to the block class
        result.addInstancePrimitive(SBlock.getEvaluationPrimitive(numberOfArguments, this));
        // Insert the block class into the dictionary of globals
        setGlobal(name, result);
        // Return the loaded block class
        return result;
    }

    public SClass loadClass(SSymbol name)
    {
        // Check if the requested class is already in the dictionary of globals
        if (hasGlobal(name)) return (SClass)getGlobal(name);
        // Load the class
        var result = loadClass(name, null);
        // Load primitives (if necessary) and return the resulting class
        if (result != null && result.hasPrimitives())
            result.loadPrimitives();
        this.setGlobal(name, result);
        return result;
    }

    public void loadSystemClass(SClass systemClass)
    {
        var result = loadClass(systemClass.getName(), systemClass);
        if (result == null) {
            throw new ProgramDefinitionError("Failed to load the "
                + systemClass.getName().getEmbeddedString() + " class."
                + " This is unexpected and may indicate that the classpath is not set correctly,"
                + " or that the core library is not available.");
        }

        // Load primitives if necessary
        if (result.hasPrimitives()) result.loadPrimitives();
    }

    private SClass loadClass(SSymbol name, SClass systemClass)
    {
        // Try loading the class from all different paths
        foreach (string cpEntry in classPaths) {
            var fn = Path.Combine(
                cpEntry,name.getEmbeddedString()+ ".som");
            if (!File.Exists(fn)) continue;
            try
            {
                // Load the class from a file and return the loaded class
                var result = SourcecodeCompiler.compileClass(cpEntry,
                    name.getEmbeddedString(), systemClass, this);
                if (dumpBytecodes)
                {
                    Disassembler.dump(result.getSOMClass(), this);
                    Disassembler.dump(result, this);
                }
                return result;

            }
            catch (IOException e)
            {
                // Continue trying different paths
            }
        }

        // The class could not be found.
        return null;
    }

    public SClass loadShellClass(string stmt)
    {
        // java.io.ByteArrayInputStream in = new
        // java.io.ByteArrayInputStream(stmt.getBytes());
        // Load the class from a stream and return the loaded class
        try {
            var result = SourcecodeCompiler.compileClass(stmt, null, this);
            if (dumpBytecodes) Disassembler.dump(result, this);
            return result;
        } catch (ProgramDefinitionError e) {
            errorExit(e.ToString());
            throw e;
        }
    }

    public static void errorPrint(string msg)
    {
        // Checkstyle: stop
        Console.Error.Write(msg);
        // Checkstyle: resume
    }

    public static void errorPrintln(string msg)
    {
        // Checkstyle: stop
        Console.Error.WriteLine(msg);
        // Checkstyle: resume
    }

    public static void errorPrintln()
    {
        // Checkstyle: stop
        Console.Error.WriteLine();
        // Checkstyle: resume
    }

    public static void print(string msg)
    {
        // Checkstyle: stop
        Console.Error.Write(msg);
        // Checkstyle: resume
    }

    public static void println(string msg)
    {
        // Checkstyle: stop
        Console.Error.WriteLine(msg);
        // Checkstyle: resume
    }

    public static void println()
    {
        // Checkstyle: stop
        Console.Error.WriteLine();
        // Checkstyle: resume
    }

    public SObject nilObject;
    public SObject trueObject;
    public SObject falseObject;
    public SClass objectClass;
    public SClass classClass;
    public SClass metaclassClass;
    public SClass nilClass;
    public SClass integerClass;
    public SClass arrayClass;
    public SClass methodClass;
    public SClass symbolClass;
    public SClass primitiveClass;
    public SClass stringClass;
    public SClass systemClass;
    public SClass blockClass;
    public SClass doubleClass;
    public SClass trueClass;
    public SClass falseClass;
    protected Dictionary<SSymbol, SAbstractObject> globals = new();
    protected List<string> classPaths;
    protected bool dumpBytecodes;
    public static string pathSeparator;
    public static string fileSeparator;
    protected Interpreter interpreter;
    protected Dictionary<string, SSymbol> symbolTable;
    // TODO: this is not how it is supposed to be... it is just a hack to cope
    // with the use of system.exit in SOM to enable testing
    protected bool avoidExit;
    protected int lastExitCode;
    protected static Universe current;
}
