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
using System.Numerics;

public class IntegerPrimitives : Primitives
{
    public IntegerPrimitives(Universe universe) : base(universe) {}

    public class AsStringPrimitive : SPrimitive
    {
        public AsStringPrimitive(Universe universe)
            : base("asString", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SNumber)frame.pop();
            frame.push(self.primAsString(universe));
        }
    }
    public class AsDoublePrimitive : SPrimitive
    {
        public AsDoublePrimitive(Universe universe)
            : base("asDouble", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SNumber)frame.pop();
            frame.push(self.primAsDouble(universe));
        }
    }
    public class AtRandomPrimitive : SPrimitive
    {
        public AtRandomPrimitive(Universe universe)
            : base("atRandom", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SInteger)frame.pop();
            frame.push(universe.newInteger(
                (long)(self.getEmbeddedInteger() * Random.Shared.NextDouble())));
        }
    }
    public class SqrtPrimitive : SPrimitive
    {
        public SqrtPrimitive(Universe universe)
            : base("sqrt", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SInteger)frame.pop();
            frame.push(self.primSqrt(universe));
        }
    }
    public class AddPrimitive : SPrimitive
    {
        public AddPrimitive(Universe universe)
            : base("+", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primAdd(right, universe));
        }
    }
    public class SubPrimitive : SPrimitive
    {
        public SubPrimitive(Universe universe)
            : base("-", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primSubtract(right, universe));
        }
    }
    public class MulPrimitive : SPrimitive
    {
        public MulPrimitive(Universe universe)
            : base("*", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primMultiply(right, universe));
        }
    }
    public class DoubleDivPrimitive : SPrimitive
    {
        public DoubleDivPrimitive(Universe universe)
            : base("//", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primDoubleDivide(right, universe));
        }
    }
    public class FromStringPrimitive : SPrimitive
    {
        public FromStringPrimitive(Universe universe)
            : base("fromString:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var param = (SString)frame.pop();
            frame.pop();

            if (long.TryParse(param.getEmbeddedString(), out var result))
            {
                frame.push(universe.newInteger(result));
            }
            else
            {
                frame.push(new SBigInteger(BigInteger.Parse(param.getEmbeddedString())));
            }
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
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primModulo(right, universe));
        }
    }
    public class RemainderPrimitive : SPrimitive
    {
        public RemainderPrimitive(Universe universe)
            : base("rem:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SInteger)frame.pop();
            frame.push(left.primRemainder(right, universe));
        }
    }
    public class PrimBitAndPrimitive : SPrimitive
    {
        public PrimBitAndPrimitive(Universe universe)
            : base("&", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primBitAnd(right, universe));
        }
    }
    public class PrimEqualPrimitive : SPrimitive
    {
        public PrimEqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primEqual(right, universe));
        }
    }
    public class PrimBitXorPrimitive : SPrimitive
    {
        public PrimBitXorPrimitive(Universe universe)
            : base("bitXor:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primBitXor(right, universe));
        }
    }

    public class PrimLessThanPrimitive : SPrimitive
    {
        public PrimLessThanPrimitive(Universe universe)
            : base("<", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primLessThan(right, universe));
        }
    }
    public class PrimLeftShiftPrimitive : SPrimitive
    {
        public PrimLeftShiftPrimitive(Universe universe)
            : base("<<", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.pop();
            var left = (SNumber)frame.pop();
            frame.push(left.primLeftShift(right, universe));
        }
    }
    public class PrimRightShiftPrimitive : SPrimitive
    {
        public PrimRightShiftPrimitive(Universe universe)
            : base(">>>", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SInteger)frame.pop();
            var rcvr = (SInteger)frame.pop();
            frame.push(
                universe.newInteger(rcvr.getEmbeddedInteger() >> (int)right.getEmbeddedInteger()));
        }
    }
    public class As32BitSignedValuePrimitive : SPrimitive
    {
        public As32BitSignedValuePrimitive(Universe universe)
            : base("as32BitSignedValue", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SNumber)frame.pop();

            frame.push(universe.newInteger(
                rcvr is SInteger si ? si.getEmbeddedInteger() : (int)((SBigInteger)rcvr).getEmbeddedBiginteger())
                );
        }
    }
    public class As32BitUnsignedValuePrimitive : SPrimitive
    {
        public As32BitUnsignedValuePrimitive(Universe universe)
            : base("as32BitUnsignedValue", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SInteger)frame.pop();
            frame.push(universe.newInteger((long)(ulong)rcvr.getEmbeddedInteger()));
        }
    }

    public override void installPrimitives()
    {
        this.installInstancePrimitive(new AsStringPrimitive(universe));
        this.installInstancePrimitive(new AsDoublePrimitive(universe));
        this.installInstancePrimitive(new AtRandomPrimitive(universe));
        this.installInstancePrimitive(new SqrtPrimitive(universe));
        this.installInstancePrimitive(new AddPrimitive(universe));
        this.installInstancePrimitive(new SubPrimitive(universe));
        this.installInstancePrimitive(new MulPrimitive(universe));
        this.installInstancePrimitive(new DoubleDivPrimitive(universe));
        this.installInstancePrimitive(new IntegerDivPrimitive(universe));
        this.installInstancePrimitive(new FromStringPrimitive(universe));
        this.installInstancePrimitive(new ModuloPrimitive(universe));
        this.installInstancePrimitive(new RemainderPrimitive(universe));
        this.installInstancePrimitive(new PrimBitAndPrimitive(universe));
        this.installInstancePrimitive(new PrimEqualPrimitive(universe));
        this.installInstancePrimitive(new PrimBitXorPrimitive(universe));
        this.installInstancePrimitive(new PrimLessThanPrimitive(universe));
        this.installInstancePrimitive(new PrimLeftShiftPrimitive(universe));
        this.installInstancePrimitive(new PrimRightShiftPrimitive(universe));
        this.installInstancePrimitive(new As32BitSignedValuePrimitive(universe));
        this.installInstancePrimitive(new As32BitUnsignedValuePrimitive(universe));
    }
}
