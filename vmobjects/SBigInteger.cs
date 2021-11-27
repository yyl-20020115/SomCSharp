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

namespace Som.VMObject;
using Som.VM;
using System.Numerics;

public class SBigInteger : SNumber
{
    // Private variable holding the embedded big integer
    protected BigInteger embeddedBiginteger;

    public SBigInteger(BigInteger value) => embeddedBiginteger = value;

    public BigInteger getEmbeddedBiginteger() => embeddedBiginteger;
    // Get the embedded big integer

    public override string ToString() => base.ToString() + "(" + embeddedBiginteger + ")";

    public override SClass getSOMClass(Universe universe) => universe.integerClass;

    public override SString primAsString(Universe universe) => universe.newString(embeddedBiginteger.ToString());

    public override SNumber primAsDouble(Universe universe) => universe.newDouble(((double)embeddedBiginteger));

    private SNumber asSNumber(BigInteger result, Universe universe) => result.GetBitLength() >= sizeof(long) ? universe.newBigInteger(result) : universe.newInteger(((long)result));

    private BigInteger asBigInteger(SNumber right) => right is SInteger si ? new BigInteger(si.getEmbeddedInteger()) : ((SBigInteger)right).embeddedBiginteger;

    public override SNumber primAdd(SNumber right, Universe universe) => right is SDouble d
            ? universe.newDouble(
                ((double)embeddedBiginteger) + d.getEmbeddedDouble())
            : asSNumber(embeddedBiginteger + asBigInteger(right), universe);


    public override SNumber primSubtract(SNumber right, Universe universe) => right is SDouble d
            ? universe.newDouble(
                ((double)embeddedBiginteger) - d.getEmbeddedDouble())
            : asSNumber(embeddedBiginteger - asBigInteger(right), universe);

    public override SNumber primMultiply(SNumber right, Universe universe) => right is SDouble
            ? universe.newDouble(
                ((double)embeddedBiginteger) * ((SDouble)right).getEmbeddedDouble())
            : asSNumber(embeddedBiginteger * asBigInteger(right), universe);

    public override SNumber primDoubleDivide(SNumber right, Universe universe)
    {
        var r = right is SInteger ? ((SInteger)right).getEmbeddedInteger() : (double)((SBigInteger)right).embeddedBiginteger;
        return universe.newDouble(((double)embeddedBiginteger) / r);
    }

    public override SNumber primIntegerDivide(SNumber right, Universe universe) => asSNumber(embeddedBiginteger / asBigInteger(right), universe);

    public override SNumber primModulo(SNumber right, Universe universe) => asSNumber(embeddedBiginteger % asBigInteger(right), universe);

    public override SNumber primSqrt(Universe universe)
    {
        var result = Math.Sqrt((double)embeddedBiginteger);
        return result == Math.Round(result) ? intOrBigInt(result, universe) : universe.newDouble(result);
    }
    public override SNumber primBitAnd(SNumber right, Universe universe) => asSNumber(embeddedBiginteger & asBigInteger(right), universe);

    public override SObject primEqual(SAbstractObject right, Universe universe) => right is not SNumber
            ? universe.falseObject
            : embeddedBiginteger.CompareTo(asBigInteger((SNumber)right)) == 0 ? universe.trueObject : universe.falseObject;

    public override SObject primLessThan(SNumber right, Universe universe) => embeddedBiginteger.CompareTo(asBigInteger(right)) < 0 ? universe.trueObject : universe.falseObject;

    public override SNumber primLeftShift(SNumber right, Universe universe) => universe.newBigInteger(embeddedBiginteger >> ((int)asBigInteger(right)));

    public override SNumber primBitXor(SNumber right, Universe universe) => asSNumber(embeddedBiginteger ^ (asBigInteger(right)), universe);
}
