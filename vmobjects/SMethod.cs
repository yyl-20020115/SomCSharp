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
using Som.Interpreter;
using Som.VM;

public class SMethod : SAbstractObject, SInvokable {

    public SMethod(SSymbol signature, int numberOfBytecodes,
        int numberOfLocals, int maxNumStackElements,
        List<SAbstractObject> literals)
    {
        this.signature = signature;
        this.numberOfLocals = numberOfLocals;
        this.bytecodes = new byte[numberOfBytecodes];
        inlineCacheClass = new SClass[numberOfBytecodes];
        inlineCacheInvokable = new SInvokable[numberOfBytecodes];
        maximumNumberOfStackElements = maxNumStackElements;
        this.literals = literals?.ToArray();
    }
    public virtual bool isPrimitive() => false;

    public int getNumberOfLocals() => numberOfLocals;

    public int getMaximumNumberOfStackElements() => maximumNumberOfStackElements;

    public SSymbol getSignature() => signature;
    public SClass getHolder() => holder;

    public void setHolder(SClass value)
    {
        holder = value;

        if (literals == null) return;

        // Make sure all nested invokables have the same holder
        for (int i = 0; i < literals.Length; i++)
            if (literals[i] is SInvokable s)
                s.setHolder(value);
    }


    public SAbstractObject getConstant(int bytecodeIndex) =>
        // Get the constant associated to a given bytecode index
        literals[bytecodes[bytecodeIndex + 1]];

    public int getNumberOfArguments() =>
        // Get the number of arguments of this method
        getSignature().getNumberOfSignatureArguments();

    public int getNumberOfBytecodes() =>
        // Get the number of bytecodes in this method
        bytecodes.Length;

    public byte getBytecode(int index) =>
        // Get the bytecode at the given index
        bytecodes[index];

    public void setBytecode(int index, byte value) =>
        // Set the bytecode at the given index to the given value
        bytecodes[index] = value;

    public void invoke(Frame frame, Interpreter interpreter)
    {
        // Allocate and push a new frame on the interpreter stack
        var newFrame = interpreter.pushNewFrame(this);
        newFrame.copyArgumentsFrom(frame);
    }

    public override string ToString() => "Method(" + getHolder().getName().getEmbeddedString() + ">>" + getSignature().ToString() + ")";

    public SClass getInlineCacheClass(int bytecodeIndex) => inlineCacheClass[bytecodeIndex];

    public SInvokable getInlineCacheInvokable(int bytecodeIndex) => inlineCacheInvokable[bytecodeIndex];

    public void setInlineCache(int bytecodeIndex, SClass receiverClass,SInvokable invokable)
    {
        inlineCacheClass[bytecodeIndex] = receiverClass;
        inlineCacheInvokable[bytecodeIndex] = invokable;
    }

    public override SClass getSOMClass(Universe universe) => universe.methodClass;

    // Private variable holding byte array of bytecodes
    protected byte[] bytecodes;
    protected SClass[] inlineCacheClass;
    protected SInvokable[] inlineCacheInvokable;
    protected SAbstractObject[] literals;
    protected SSymbol signature;
    protected SClass holder;
    // Meta information
    protected int numberOfLocals;
    protected int maximumNumberOfStackElements;
}
