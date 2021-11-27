/**
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

namespace Som.Primitives;
using Som.Compiler;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;
using System.Diagnostics;

public class SystemPrimitives : Primitives
{
    public SystemPrimitives(Universe universe) : base(universe) { }

    public class LoadPrimitive : SPrimitive
    {
        public LoadPrimitive(Universe universe)
            : base("load:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SSymbol)frame.pop();
            frame.pop(); // not required
            SClass result = null;
            try
            {
                result = universe.loadClass(argument);
            }
            catch (ProgramDefinitionError e)
            {
                universe.errorExit(e.ToString());
            }
            frame.push(result != null ? result : universe.nilObject);
        }
    }
    public class ExitPrimitive : SPrimitive
    {
        public ExitPrimitive(Universe universe)
            : base("exit:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var error = (SInteger)frame.pop();
            universe.exit(error.getEmbeddedInteger());
        }
    }
    public class GlobalPrimitive : SPrimitive
    {
        public GlobalPrimitive(Universe universe)
            : base("global", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SSymbol)frame.pop();
            frame.pop(); // not required
            var result = universe.getGlobal(argument);
            frame.push(result != null ? result : universe.nilObject);
        }
    }
    public class GlobalPutPrimitive : SPrimitive
    {
        public GlobalPutPrimitive(Universe universe)
            : base("global:put", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var value = frame.pop();
            var argument = (SSymbol)frame.pop();
            universe.setGlobal(argument, value);
        }
    }
    public class PrintStringPrimitive : SPrimitive
    {
        public PrintStringPrimitive(Universe universe)
            : base("printString:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.pop();
            Universe.print(argument.getEmbeddedString());
        }
    }
    public class PrintNewlinePrimitive : SPrimitive
    {
        public PrintNewlinePrimitive(Universe universe)
            : base("printNewline", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            Universe.println("");
        }
    }
    public class ErrorPrintPrimitive : SPrimitive
    {
        public ErrorPrintPrimitive(Universe universe)
            : base("errorPrint:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.pop();
            Universe.errorPrint(argument.getEmbeddedString());
        }
    }
    public class ErrorPrintlnPrimitive : SPrimitive
    {
        public ErrorPrintlnPrimitive(Universe universe)
            : base("errorPrintln:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.pop();
            Universe.errorPrintln(argument.getEmbeddedString());
        }
    }
    public class PrintStackTracePrimitive : SPrimitive
    {
        public PrintStackTracePrimitive(Universe universe)
            : base("printStackTrace", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            frame.pop();
            frame.printStackTrace();
            frame.push(universe.trueObject);
        }
    }
    public class LoadFilePrimitive : SPrimitive
    {
        public LoadFilePrimitive(Universe universe)
            : base("loadFile:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var fileName = (SString)frame.pop();
            frame.pop();

            var p = fileName.getEmbeddedString();
            try
            {
                frame.push(universe.newString(File.ReadAllText(p)));
            }
            catch (IOException e)
            {
                frame.push(universe.nilObject);
            }
        }
    }
    public class FullGCPrimitive : SPrimitive
    {
        public FullGCPrimitive(Universe universe)
            : base("fullGC", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            frame.pop();
            GC.Collect();
            frame.push(universe.trueObject);
        }
    }
    public class TicksPrimitive : SPrimitive
    {
        protected SystemPrimitives sp;
        public TicksPrimitive(Universe universe, SystemPrimitives sp)
            : base("ticks", universe) { this.sp = sp; }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            frame.pop(); // ignore
            int time = (int)(Stopwatch.GetTimestamp() * 10 - sp.startMicroTime);
            frame.push(universe.newInteger(time));
        }
    }
    public class TimePrimitive : SPrimitive
    {
        protected SystemPrimitives sp;
        public TimePrimitive(Universe universe,SystemPrimitives sp)
            : base("time", universe) { this.sp = sp; }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            frame.pop(); // ignore
            int time = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - sp.startTime);
            frame.push(universe.newInteger(time));
        }
    }
    public override void installPrimitives()
    {
        this.installInstancePrimitive(new LoadPrimitive(universe));
        this.installInstancePrimitive(new ExitPrimitive(universe));
        this.installInstancePrimitive(new GlobalPrimitive(universe));
        this.installInstancePrimitive(new GlobalPutPrimitive(universe));
        this.installInstancePrimitive(new PrintStringPrimitive(universe));
        this.installInstancePrimitive(new PrintNewlinePrimitive(universe));
        this.installInstancePrimitive(new ErrorPrintPrimitive(universe));
        this.installInstancePrimitive(new ErrorPrintlnPrimitive(universe));
        this.installInstancePrimitive(new PrintStackTracePrimitive(universe));
        this.installInstancePrimitive(new LoadFilePrimitive(universe));
        this.installInstancePrimitive(new FullGCPrimitive(universe));
        this.installInstancePrimitive(new TicksPrimitive(universe,this));
        this.installInstancePrimitive(new TimePrimitive(universe,this));

        this.startMicroTime = Stopwatch.GetTimestamp() * 10;
        this.startTime = this.startMicroTime / 1000L;        
    }

    protected long startTime;
    protected long startMicroTime;
}
