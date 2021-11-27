namespace Som.VMObject;
using Som.VM;
using System.Numerics;

public abstract class SNumber : SAbstractObject
{
    public abstract SString primAsString(Universe universe);

    public abstract SNumber primAsDouble(Universe universe);

    public abstract SNumber primSqrt(Universe universe);

    public abstract SNumber primAdd(SNumber right, Universe universe);

    public abstract SNumber primSubtract(SNumber right, Universe universe);

    public abstract SNumber primMultiply(SNumber right, Universe universe);

    public abstract SNumber primDoubleDivide(SNumber right, Universe universe);

    public abstract SNumber primIntegerDivide(SNumber right, Universe universe);

    public abstract SNumber primModulo(SNumber right, Universe universe);

    public abstract SNumber primBitAnd(SNumber right, Universe universe);

    public abstract SNumber primBitXor(SNumber right, Universe universe);

    public abstract SNumber primLeftShift(SNumber right, Universe universe);

    public abstract SObject primEqual(SAbstractObject right, Universe universe);

    public abstract SObject primLessThan(SNumber right, Universe universe);

    protected SNumber intOrBigInt(double value, Universe universe) => value > long.MaxValue || value < long.MinValue
            ? universe.newBigInteger(new BigInteger(Math.Round(value)))
            : universe.newInteger((long)Math.Round(value));

    protected SObject asSbool(bool result, Universe universe) => result ? universe.trueObject : universe.falseObject;

}
