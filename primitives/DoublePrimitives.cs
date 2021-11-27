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

public class DoublePrimitives : Primitives
{
    public DoublePrimitives(Universe universe) : base(universe) { }
    public class AsStringPrimitive : SPrimitive
    {
        public AsStringPrimitive(Universe universe)
            : base("asString", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.pop();
            frame.push(self.primAsString(universe));
        }
    }
    public class AsIntegerPrimitive : SPrimitive
    {
        public AsIntegerPrimitive(Universe universe)
            : base("asInteger", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.pop();
            frame.push(self.primAsInteger(universe));
        }
    }
    public class SqrtPrimitive : SPrimitive
    {
        public SqrtPrimitive(Universe universe)
            : base("sqrt", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.pop();
            frame.push(self.primSqrt(universe));
        }
    }
    public class AddPrimitive : SPrimitive
    {
        public AddPrimitive(Universe universe)
            : base("+", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primAdd(op1, universe));
        }
    }
    public class SubPrimitive : SPrimitive
    {
        public SubPrimitive(Universe universe)
            : base("-", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primSubtract(op1, universe));
        }
    }
    public class MulPrimitive : SPrimitive
    {
        public MulPrimitive(Universe universe)
            : base("*", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primMultiply(op1, universe));
        }
    }
    public class DoubleDivPrimitive : SPrimitive
    {
        public DoubleDivPrimitive(Universe universe)
            : base("//", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primDoubleDivide(op1, universe));
        }
    }
    public class FromStringPrimitive : SPrimitive
    {
        public FromStringPrimitive(Universe universe)
            : base("fromString:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var arg = (SString)frame.pop();
            frame.pop();

            if (!double.TryParse(arg.getEmbeddedString(), out var d)) d = double.NaN;

            frame.push(universe.newDouble(d));
        }
    }
    public class IntegerDivPrimitive : SPrimitive
    {
        public IntegerDivPrimitive(Universe universe)
            : base("/", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primIntegerDivide(right, universe));
        }
    }
    public class ModuloPrimitive : SPrimitive
    {
        public ModuloPrimitive(Universe universe)
            : base("%", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primModulo(op1, universe));
        }
    }
    public class RoundPrimitive : SPrimitive
    {
        public RoundPrimitive(Universe universe)
            : base("round", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.pop();
            var result = (long)Math.Round(rcvr.getEmbeddedDouble());
            frame.push(universe.newInteger(result));
        }
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primEqual(op1, universe));
        }
    }
    public class LessThanPrimitive : SPrimitive
    {
        public LessThanPrimitive(Universe universe)
            : base("<", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.pop();
            var op2 = (SDouble)frame.pop();
            frame.push(op2.primLessThan(op1, universe));
        }
    }
    public class SinPrimitive : SPrimitive
    {
        public SinPrimitive(Universe universe)
            : base("sin", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.pop();
            var result = Math.Sin(rcvr.getEmbeddedDouble());
            frame.push(universe.newDouble(result));
        }
    }
    public class CosPrimitive : SPrimitive
    {
        public CosPrimitive(Universe universe)
            : base("cos", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.pop();
            var result = Math.Cos(rcvr.getEmbeddedDouble());
            frame.push(universe.newDouble(result));
        }
    }
    public class PositiveInfinityPrimitive : SPrimitive
    {
        public PositiveInfinityPrimitive(Universe universe)
            : base("PositiveInfinity", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            frame.pop();
            frame.push(universe.newDouble(double.PositiveInfinity));
        }
    }
    public override void installPrimitives()
    {
        this.installInstancePrimitive(new AsStringPrimitive(universe));
        this.installInstancePrimitive(new AsIntegerPrimitive(universe));
        this.installInstancePrimitive(new FromStringPrimitive(universe));
        this.installInstancePrimitive(new SqrtPrimitive(universe));
        this.installInstancePrimitive(new AddPrimitive(universe));
        this.installInstancePrimitive(new SubPrimitive(universe));
        this.installInstancePrimitive(new MulPrimitive(universe));
        this.installInstancePrimitive(new DoubleDivPrimitive(universe));
        this.installInstancePrimitive(new ModuloPrimitive(universe));
        this.installInstancePrimitive(new EqualPrimitive(universe));
        this.installInstancePrimitive(new LessThanPrimitive(universe));
        this.installInstancePrimitive(new RoundPrimitive(universe));
        this.installInstancePrimitive(new SinPrimitive(universe));
        this.installInstancePrimitive(new CosPrimitive(universe));
        this.installInstancePrimitive(new PositiveInfinityPrimitive(universe));
    }
}
