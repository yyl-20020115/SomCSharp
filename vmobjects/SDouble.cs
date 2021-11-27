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

public class SDouble : SNumber
{
    protected double embeddedDouble;

    public SDouble(double value) => embeddedDouble = value;

    public double getEmbeddedDouble() =>
        // Get the embedded double
        embeddedDouble;
    public override string ToString() => base.ToString() + "(" + embeddedDouble + ")";

    public override SClass getSOMClass(Universe universe) => universe.doubleClass;

    private double coerceToDouble(SNumber o, Universe universe) => o is SDouble d
            ? d.embeddedDouble
            : o is SInteger i
                ? i.getEmbeddedInteger()
                : o is SBigInteger s ? (double)s.getEmbeddedBiginteger() 
                : throw new Exception("Cannot coerce to Double!");

    public override SString primAsString(Universe universe) => universe.newString(embeddedDouble.ToString());

    public override SDouble primAsDouble(Universe universe) => this;

    public SInteger primAsInteger(Universe universe) => universe.newInteger((long)embeddedDouble);

    public override SNumber primSqrt(Universe universe) => universe.newDouble(Math.Sqrt(embeddedDouble));

    public override SNumber primAdd(SNumber right, Universe universe) => universe.newDouble(embeddedDouble + coerceToDouble(right, universe));

    public override SNumber primSubtract(SNumber right, Universe universe) => universe.newDouble(embeddedDouble - coerceToDouble(right, universe));

    public override SNumber primMultiply(SNumber right, Universe universe) => universe.newDouble(embeddedDouble * coerceToDouble(right, universe));

    public override SNumber primDoubleDivide(SNumber right, Universe universe) => universe.newDouble(embeddedDouble / coerceToDouble(right, universe));

    public override SNumber primIntegerDivide(SNumber right, Universe universe) => throw new RuntimeException("not yet implemented, SOM doesn't offer it");

    public override SNumber primModulo(SNumber right, Universe universe) => universe.newDouble(embeddedDouble % coerceToDouble(right, universe));

    public override SNumber primBitAnd(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SNumber primBitXor(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SNumber primLeftShift(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SObject primEqual(SAbstractObject right, Universe universe) => right is not SNumber ? universe.falseObject : asSbool(embeddedDouble == coerceToDouble((SNumber)right, universe), universe);

    public override SObject primLessThan(SNumber right, Universe universe) => asSbool(embeddedDouble < coerceToDouble(right, universe), universe);
}
