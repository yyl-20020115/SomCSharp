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
namespace Som.VMObject;
using Som.VM;
using System.Numerics;

public class SInteger : SNumber
{
    /**
     * Language convention requires integers up to this value to be identical.
     */

    /**
     * Cache to store integers up to {@link #MAX_IDENTICAL_INT}.
     */
    private static Dictionary<long, SInteger> CACHE = new();

    // Private variable holding the embedded integer
    private long embeddedInteger;

    private SInteger(long value) => embeddedInteger = value;

    public static SInteger getInteger(long value) 
        => value > int.MaxValue ? new SInteger(value) : !CACHE.ContainsKey(value) ? (CACHE[value] = new SInteger(value)) : CACHE[value];

    public long getEmbeddedInteger() => embeddedInteger;
    // Get the embedded integer

    public override string ToString() => base.ToString() + "(" + embeddedInteger + ")";

    public override SClass getSOMClass(Universe universe) => universe.integerClass;

    public override SString primAsString(Universe universe) => universe.newString(embeddedInteger.ToString());

    public override SNumber primAsDouble(Universe universe) => universe.newDouble(embeddedInteger);

    public override SNumber primSqrt(Universe universe)
    {
        var result = Math.Sqrt(embeddedInteger);

        return result == Math.Round(result) ? intOrBigInt(result, universe) : universe.newDouble(result);
    }

    public override SNumber primAdd(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.newBigInteger(new BigInteger(embeddedInteger) + (
                s.getEmbeddedBiginteger()));
        }
        else if (right is SDouble d)
        {
            return universe.newDouble(embeddedInteger + d.getEmbeddedDouble());
        }
        else if(right is SInteger r)
        {
            try
            {
                return universe.newInteger(embeddedInteger + r.getEmbeddedInteger());
            }
            catch
            {
                return universe.newBigInteger(new BigInteger(embeddedInteger) + (
                    new BigInteger(r.getEmbeddedInteger())));
            }
        }
        else
            throw new InvalidOperationException();
    }

    public override SNumber primSubtract(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.newBigInteger(new BigInteger(embeddedInteger) - (
                s.getEmbeddedBiginteger()));
        }
        else if (right is SDouble d)
        {
            return universe.newDouble(embeddedInteger - d.getEmbeddedDouble());
        }
        else if (right is SInteger r)
        {
            try
            {
                return universe.newInteger(embeddedInteger - r.getEmbeddedInteger());
            }
            catch
            {
                return universe.newBigInteger(new BigInteger(embeddedInteger) - (
                    new BigInteger(r.getEmbeddedInteger())));
            }
        }
        else
            throw new InvalidOperationException();
    }

    public override SNumber primMultiply(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.newBigInteger(new BigInteger(embeddedInteger) * (
                s.getEmbeddedBiginteger()));
        }
        else if (right is SDouble d)
        {
            return universe.newDouble(embeddedInteger * d.getEmbeddedDouble());
        }
        else if (right is SInteger r)
        {
            try
            {
                return universe.newInteger(embeddedInteger * r.getEmbeddedInteger());
            }
            catch
            {
                return universe.newBigInteger(new BigInteger(embeddedInteger) * (
                    new BigInteger(r.getEmbeddedInteger())));
            }
        }
        else
            throw new InvalidOperationException();
    }


    public override SNumber primDoubleDivide(SNumber right, Universe universe)
    {
        var result = 0.0;

        if (right is SBigInteger s) {
            result = embeddedInteger / ((double)s.getEmbeddedBiginteger());
        } else if (right is SDouble d) {
            result = embeddedInteger / d.getEmbeddedDouble();
        } 
        else if(right is SInteger i)
        {
            result = (double)embeddedInteger / i.getEmbeddedInteger();
        }

        return universe.newDouble(result);
    }

    public override SNumber primIntegerDivide(SNumber right, Universe universe) =>
        right is SBigInteger s
            ? (global::Som.VMObject.SNumber)universe.newBigInteger(new BigInteger(embeddedInteger) / (
                s.getEmbeddedBiginteger()))
            : (global::Som.VMObject.SNumber)(right is SDouble d
                ? universe.newInteger((long)(embeddedInteger / d.getEmbeddedDouble()))
                : universe.newInteger(embeddedInteger / ((SInteger)right).getEmbeddedInteger()));

    public override SNumber primModulo(SNumber right, Universe universe) =>
        // Note: modulo semantics of SOM differ from Java, with respect to
        // negative operands, but BigInteger doesn't support a negative
        // second operand, so, we should get an exception, which we can
        // properly handle once an application actually needs it.
        right is SBigInteger s
            ? (global::Som.VMObject.SNumber)universe.newBigInteger(new BigInteger(embeddedInteger) % (
                s.getEmbeddedBiginteger()))
            : (global::Som.VMObject.SNumber)(right is SDouble d
                ? universe.newDouble(embeddedInteger % d.getEmbeddedDouble())
                : universe.newInteger(
                            (long)Math.Floor((double)(embeddedInteger % ((SInteger)right).getEmbeddedInteger()))));

    public SInteger primRemainder(SNumber right, Universe universe) => right is SInteger s ? universe.newInteger(embeddedInteger % s.embeddedInteger) : throw new InvalidOperationException();
    public override SNumber primBitAnd(SNumber right, Universe universe) => right is SBigInteger s
            ? universe.newBigInteger(new BigInteger(embeddedInteger) & (
                s.getEmbeddedBiginteger()))
            : (SNumber)universe.newInteger(embeddedInteger & ((SInteger)right).embeddedInteger);

    public override SNumber primBitXor(SNumber right, Universe universe) => right is SBigInteger s
            ? universe.newBigInteger(new BigInteger(embeddedInteger) ^ (
                s.getEmbeddedBiginteger()))
            : (SNumber)universe.newInteger(embeddedInteger ^ ((SInteger)right).embeddedInteger);
    public static int LeadingZeros(long value)
    {
        // Shift right unsigned to work with both positive and negative values
        var uValue = (ulong)value;
        int leadingZeros = 0;
        while (uValue != 0)
        {
            uValue >>= 1;
            leadingZeros++;
        }

        return (64 - leadingZeros);
    }
    public static int LeadingZeros(int value)
    {
        // Shift right unsigned to work with both positive and negative values
        var uValue = (uint)value;
        int leadingZeros = 0;
        while (uValue != 0)
        {
            uValue >>= 1;
            leadingZeros++;
        }

        return (32 - leadingZeros);
    }

    public override SNumber primLeftShift(SNumber right, Universe universe)
    {
        var r = ((SInteger)right).embeddedInteger;
        //assert r > 0;
        return sizeof(long) * 8 - LeadingZeros(embeddedInteger) + r > sizeof(long) * 8 - 1
            ? universe.newBigInteger(new BigInteger(embeddedInteger) << ((int)r))
            : universe.newInteger((embeddedInteger << (int)r));
    }

    public override SObject primEqual(SAbstractObject right, Universe universe)
    {
        var result = right is SBigInteger s
            ? new BigInteger(embeddedInteger).Equals(
                s.getEmbeddedBiginteger())
            : right is SDouble d
                ? embeddedInteger == d.getEmbeddedDouble()
                : right is SInteger i ? embeddedInteger == i.getEmbeddedInteger() : false;
        return asSbool(result, universe);
    }

    public override SObject primLessThan(SNumber right, Universe universe)
    {
        var result = right is SBigInteger s
            ? new BigInteger(embeddedInteger).CompareTo(
                s.getEmbeddedBiginteger()) < 0
            : right is SDouble d ? embeddedInteger < d.getEmbeddedDouble() : embeddedInteger < ((SInteger)right).getEmbeddedInteger();
        return asSbool(result, universe);
    }
}
