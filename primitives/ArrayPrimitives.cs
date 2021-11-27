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
using Som.Interpreter;
using Som.VM;
using Som.VMObject;
public class ArrayPrimitives : Primitives
{

    public ArrayPrimitives(Universe universe) : base(universe) { }

    public class AtPrimitive : SPrimitive
    {
        public AtPrimitive(Universe universe)
            : base("at:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var index = (SInteger)frame.pop();
            var self = (SArray)frame.pop();
            frame.push(self.getIndexableField(index.getEmbeddedInteger() - 1));
        }
    }
    public class AtPutPrimitive : SPrimitive
    {
        public AtPutPrimitive(Universe universe)
            : base("at:put:", universe) { }
        
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var value = frame.pop();
            var index = (SInteger)frame.pop();
            var self = (SArray)frame.getStackElement(0);
            self.setIndexableField(index.getEmbeddedInteger() - 1, value);
        }
    }

    public class LengthPrimitive : SPrimitive
    {
        public LengthPrimitive(Universe universe)
            : base("length:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SArray)frame.pop();
            frame.push(universe.newInteger(self.getNumberOfIndexableFields()));
        }
    }
    public class NewPrimitive : SPrimitive
    {
        public NewPrimitive(Universe universe)
            : base("new:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var length = (SInteger)frame.pop();
            frame.pop(); // not required
            frame.push(universe.newArray(length.getEmbeddedInteger()));
        }

    }

    public override void installPrimitives()
    {
        this.installInstancePrimitive(new AtPrimitive(universe));
        this.installInstancePrimitive(new AtPutPrimitive(universe));
        this.installInstancePrimitive(new LengthPrimitive(universe));
        this.installClassPrimitive(new NewPrimitive(universe));
    }
}
