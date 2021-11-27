
/**
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
namespace Som.Primitives;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;

public class ObjectPrimitives : Primitives
{

    public ObjectPrimitives(Universe universe) : base(universe)
    {
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("==", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = frame.pop();
            var op2 = frame.pop();
            frame.push(op1 == op2 ? universe.trueObject : universe.falseObject);
        }
    }
    public class HashCodePrimitive : SPrimitive
    {
        public HashCodePrimitive(Universe universe)
            : base("hashcode", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.pop();
            frame.push(universe.newInteger(self.GetHashCode()));
        }
    }
    public class ObjectSizePrimitive : SPrimitive
    {
        public ObjectSizePrimitive(Universe universe)
            : base("objectSize", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.pop();

            // each object holds its class as an implicit member that contributes to its size
            int size = 1;
            if (self is SArray a)
            {
                size += a.getNumberOfIndexableFields();
            }
            else if (self is SObject s)
            {
                size += s.getNumberOfFields();
            }
            frame.push(universe.newInteger(size));
        }
    }

    public class PerformPrimitive : SPrimitive
    {
        public PerformPrimitive(Universe universe)
            : base("perform:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var arg = frame.pop();
            var self = frame.getStackElement(0);
            var selector = (SSymbol)arg;

            var invokable = self.getSOMClass(universe).lookupInvokable(selector);
            invokable.invoke(frame, interpreter);
        }
    }
    public class ClassPrimitive : SPrimitive
    {
        public ClassPrimitive(Universe universe)
            : base("class", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.pop();
            frame.push(self.getSOMClass(universe));
        }
    }
    public class HaltPrimitive : SPrimitive
    {
        public HaltPrimitive(Universe universe)
            : base("halt", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            Universe.errorPrintln("BREAKPOINT");
        }
    }

    public class PerformInSuperClassPrimitive : SPrimitive
    {
        public PerformInSuperClassPrimitive(Universe universe)
            : base("perform:inSuperclass:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var arg2 = frame.pop();
            var arg = frame.pop();
            // Object self = frame.getStackElement(0);

            var selector = (SSymbol)arg;
            var clazz = (SClass)arg2;

            var invokable = clazz.lookupInvokable(selector);
            invokable.invoke(frame, interpreter);
        }
    }

    public class PerformWithArgumentsPrimitive : SPrimitive
    {
        public PerformWithArgumentsPrimitive(Universe universe)
            : base("perform:withArguments:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var arg2 = frame.pop();
            var arg = frame.pop();
            var self = frame.getStackElement(0);

            var selector = (SSymbol)arg;
            var args = (SArray)arg2;

            for (int i = 0; i < args.getNumberOfIndexableFields(); i++)
            {
                frame.push(args.getIndexableField(i));
            }

            var invokable = self.getSOMClass(universe).lookupInvokable(selector);
            invokable.invoke(frame, interpreter);
        }
    }

    public class InstVarAtPrimitive : SPrimitive
    {
        public InstVarAtPrimitive(Universe universe)
            : base("instVarAt:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var arg = frame.pop();
            var self = (SObject)frame.pop();
            var idx = (SInteger)arg;

            frame.push(self.getField(idx.getEmbeddedInteger() - 1));
        }
    }

    public class InstVarAtPutPrimitive : SPrimitive
    {
        public InstVarAtPutPrimitive(Universe universe)
            : base("instVarAt:put:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var val = frame.pop();
            var arg = frame.pop();
            var self = (SObject)frame.getStackElement(0);

            var idx = (SInteger)arg;

            self.setField(idx.getEmbeddedInteger() - 1, val);
        }
    }

    public override void installPrimitives()
    {
        this.installInstancePrimitive(new EqualPrimitive(universe));
        this.installInstancePrimitive(new HashCodePrimitive(universe));
        this.installInstancePrimitive(new ObjectSizePrimitive(universe));
        this.installInstancePrimitive(new PerformPrimitive(universe));
        this.installInstancePrimitive(new ClassPrimitive(universe));
        this.installInstancePrimitive(new HaltPrimitive(universe));
        this.installInstancePrimitive(new PerformInSuperClassPrimitive(universe));
        this.installInstancePrimitive(new PerformWithArgumentsPrimitive(universe));
        this.installInstancePrimitive(new InstVarAtPrimitive(universe));
        this.installInstancePrimitive(new InstVarAtPutPrimitive(universe));
    }
}
