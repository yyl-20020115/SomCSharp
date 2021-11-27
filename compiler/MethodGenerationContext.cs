/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
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

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;
using static Som.Compiler.Parser;
using static Som.Interpreter.Bytecodes;
using System.Runtime.Serialization;

public class MethodGenerationContext
{
    protected ClassGenerationContext holderGenc;
    protected MethodGenerationContext outerGenc;

    protected SSymbol signature;
    protected List<string> arguments = new ();
    protected List<string> locals = new ();
    protected List<SAbstractObject> literals = new ();
    protected List<byte> bytecode = new();
    protected bool finished;
    protected bool primitive;
    protected bool blockMethod;

    /**
     * Constructor used for block methods.
     */
    public MethodGenerationContext(ClassGenerationContext holderGenc, MethodGenerationContext outerGenc)
    {
        this.holderGenc = holderGenc;
        this.outerGenc = outerGenc;
        this.blockMethod = outerGenc != null;
    }

    /**
     * Constructor used for normal methods.
     */
    public MethodGenerationContext(ClassGenerationContext holderGenc)
        : this(holderGenc, null) { }
    public void addArgument(string arg) => arguments.Add(arg);
    public bool isPrimitive() => primitive;
    public SInvokable assemble(Universe universe)
        => primitive ? SPrimitive.getEmptyPrimitive(signature.getEmbeddedString(), universe) : assembleMethod(universe);

    public SMethod assembleMethod(Universe universe)
    {
        // create a method instance with the given number of bytecodes
        var numLocals = locals.Count;
        var meth = universe.newMethod(signature, bytecode.Count,
            numLocals, computeStackDepth(),
            literals);

        // copy bytecodes into method
        var i = 0;
        foreach (var bc in bytecode) meth.setBytecode(i++, bc);
        // return the method - the holder field is to be set later on!
        return meth;
    }

    private int computeStackDepth()
    {
        int depth = 0;
        int maxDepth = 0;
        int i = 0;

        while (i < bytecode.Count)
        {
            switch (bytecode[(i)])
            {
                case HALT:
                    i++;
                    break;
                case DUP:
                    depth++;
                    i++;
                    break;
                case PUSH_LOCAL:
                case PUSH_ARGUMENT:
                    depth++;
                    i += 3;
                    break;
                case PUSH_FIELD:
                case PUSH_BLOCK:
                case PUSH_CONSTANT:
                case PUSH_GLOBAL:
                    depth++;
                    i += 2;
                    break;
                case POP:
                    depth--;
                    i++;
                    break;
                case POP_LOCAL:
                case POP_ARGUMENT:
                    depth--;
                    i += 3;
                    break;
                case POP_FIELD:
                    depth--;
                    i += 2;
                    break;
                case SEND:
                case SUPER_SEND:
                    {
                        // these are special: they need to look at the number of
                        // arguments (extractable from the signature)
                        var sig = (SSymbol)literals[(bytecode[(i + 1)])];
                        depth -= sig.getNumberOfSignatureArguments();
                        depth++; // return value
                        i += 2;
                        break;
                    }
                case RETURN_LOCAL:
                case RETURN_NON_LOCAL:
                    i++;
                    break;
                default:
                    throw new IllegalStateException("Illegal bytecode "
                        + bytecode[(i)]);
            }

            if (depth > maxDepth) maxDepth = depth;
        }

        return maxDepth;
    }

    public void markAsPrimitive() => primitive = true;

    public void setSignature(SSymbol sig) => signature = sig;

    public bool addArgumentIfAbsent(string arg)
    {
        if (arguments.Contains(arg)) return false;
        arguments.Add(arg);
        return true;
    }

    public bool isFinished() => finished;

    public void markAsFinished() => this.finished = false;

    public bool addLocalIfAbsent(string local)
    {
        if (locals.Contains(local)) return false;
        locals.Add(local);
        return true;
    }

    public void addLocal(string local) => locals.Add(local);

    public bool hasBytecodes() => bytecode.Count > 0;

    public void removeLastBytecode() => bytecode.RemoveAt(bytecode.Count - 1);

    public bool isBlockMethod() => blockMethod;

    public void setFinished() => finished = true;

    public bool addLiteralIfAbsent(SAbstractObject lit, Parser parser)
    {
        if (literals.Contains(lit)) return false;
        addLiteral(lit, parser);
        return true;
    }

    public ClassGenerationContext getHolder() => holderGenc;

    public byte addLiteral(SAbstractObject lit, Parser parser)
    {
        int i = literals.Count;
        if (i > Byte.MaxValue) {
            var methodSignature = holderGenc.Name.getEmbeddedString() + ">>" + signature;
            throw new ParseError(
                "The method " + methodSignature + " has more than the supported " +
                    Byte.MaxValue
                    + " literal values. Please split the method. The literal to be added is: " + lit,
                Symbol.NONE, parser);
        }
        literals.Add(lit);
        return (byte)i;
    }

    public void updateLiteral(SAbstractObject oldVal, byte index, SAbstractObject newVal) =>
        //assert literals.get(index) == oldVal;
        literals[index] = newVal;

    public bool findVar(string var, Triplet<byte, byte, bool> tri)
    {
        // triplet: index, context, isArgument
        tri.X = (byte)locals.IndexOf(var);
        if (tri.X == 0xff)
        {
            tri.X = (byte)arguments.IndexOf(var);
            if (tri.X == 0xff)
            {
                if (outerGenc == null)
                {
                    return false;
                }
                else
                {
                    tri.Y = (byte)(tri.Y + 1);
                    return outerGenc.findVar(var, tri);
                }
            }
            else
            {
                tri.Z = true;
            }
        }

        return true;
    }

    public bool hasField(SSymbol field) => holderGenc.hasField(field);

    public byte getFieldIndex(SSymbol field) => holderGenc.getFieldIndex(field);

    public int getNumberOfArguments() => arguments.Count;

    public MethodGenerationContext addBytecode(byte code)
    {
        bytecode.Add(code);
        return this;
    }

    public byte findLiteralIndex(SAbstractObject lit) => (byte)literals.IndexOf(lit);

    public MethodGenerationContext getOuter() => outerGenc;
}

[Serializable]
internal class IllegalStateException : Exception
{
    public IllegalStateException()
    {
    }

    public IllegalStateException(string? message) : base(message)
    {
    }

    public IllegalStateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected IllegalStateException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}