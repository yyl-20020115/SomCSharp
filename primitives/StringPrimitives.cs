/**
 * Copyright (c) 2013 Stefan Marr,   stefan.marr@vub.ac.be
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

public class StringPrimitives : Primitives
{

    public StringPrimitives(Universe universe) : base(universe) { }
    public class ConcatenatePrimitive : SPrimitive
    {
        public ConcatenatePrimitive(Universe universe)
            : base("concatenate:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.pop();
            var self = (SString)frame.pop();
            frame.push(universe.newString(self.getEmbeddedString()
                + argument.getEmbeddedString()));
        }
    }
    public class AsSymbolPrimitive : SPrimitive
    {
        public AsSymbolPrimitive(Universe universe)
            : base("asSymbol:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            frame.push(universe.symbolFor(self.getEmbeddedString()));
        }
    }
    public class LengthPrimitive : SPrimitive
    {
        public LengthPrimitive(Universe universe)
            : base("length", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            frame.push(universe.newInteger(self.getEmbeddedString().Length));
        }
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var op2 = frame.pop();
            var op1 = (SString)frame.pop(); // self
            if (op2 is SString)
            {
                var s = (SString)op2;
                if (s.getEmbeddedString() == (op1.getEmbeddedString()))
                {
                    frame.push(universe.trueObject);
                    return;
                }
            }

            frame.push(universe.falseObject);
        }
    }

    public class SubstringFromToPrimitive : SPrimitive
    {
        public SubstringFromToPrimitive(Universe universe)
            : base("primSubstringFrom:to:", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var end = (SInteger)frame.pop();
            var start = (SInteger)frame.pop();
            var self = (SString)frame.pop();

            try
            {
                frame.push(universe.newString(self.getEmbeddedString().Substring(
                    (int)start.getEmbeddedInteger() - 1,
                    (int)end.getEmbeddedInteger())));
            }
            catch (IndexOutOfRangeException e)
            {
                frame.push(universe.newString(
                    "Error - index out of bounds"));
            }
        }
    }
    public class HashCodePrimitive : SPrimitive
    {
        public HashCodePrimitive(Universe universe)
            : base("hashcode", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            frame.push(universe.newInteger(self.getEmbeddedString().GetHashCode()));
        }
    }
    public class IsWhiteSpacePrimitive : SPrimitive
    {
        public IsWhiteSpacePrimitive(Universe universe)
            : base("isWhiteSpace", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            var embedded = self.getEmbeddedString();

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsWhiteSpace(embedded[i]))
                {
                    frame.push(universe.falseObject);
                    return;
                }
            }

            frame.push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }
    }
    public class IsLettersPrimitives : SPrimitive
    {
        public IsLettersPrimitives(Universe universe)
            : base("isLetters", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            var embedded = self.getEmbeddedString();

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsLetter(embedded[i]))
                {
                    frame.push(universe.falseObject);
                    return;
                }
            }

            frame.push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }
    }

    public class IsDigitsPrimitive : SPrimitive
    {
        public IsDigitsPrimitive(Universe universe)
            : base("isDigits", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.pop();
            var embedded = self.getEmbeddedString();

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsDigit(embedded[i]))
                {
                    frame.push(universe.falseObject);
                    return;
                }
            }
            frame.push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }

    }
    public override void installPrimitives()
    {
        this.installInstancePrimitive(new ConcatenatePrimitive(universe));
        this.installInstancePrimitive(new AsSymbolPrimitive(universe));
        this.installInstancePrimitive(new LengthPrimitive(universe));
        this.installInstancePrimitive(new EqualPrimitive(universe));
        this.installInstancePrimitive(new SubstringFromToPrimitive(universe));
        this.installInstancePrimitive(new HashCodePrimitive(universe));
        this.installInstancePrimitive(new IsWhiteSpacePrimitive(universe));
        this.installInstancePrimitive(new IsLettersPrimitives(universe));
        this.installInstancePrimitive(new IsDigitsPrimitive(universe));
    }
}
