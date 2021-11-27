namespace Som.Primitives;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;

public class MethodPrimitives : Primitives
{
    public MethodPrimitives(Universe universe) : base(universe) { }
    public class HolderPrimitive : SPrimitive
    {
        public HolderPrimitive(Universe universe)
            : base("holder", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SMethod)frame.pop();
            frame.push(self.getHolder());
        }
    }
    public class SignaturePrimitive : SPrimitive
    {
        public SignaturePrimitive(Universe universe)
            : base("signature", universe) { }
        public override void invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SMethod)frame.pop();
            frame.push(self.getSignature());
        }
    }

    public override void installPrimitives()
    {
        this.installInstancePrimitive(new HolderPrimitive(universe));
        this.installInstancePrimitive(new SignaturePrimitive(universe));
    }
}
