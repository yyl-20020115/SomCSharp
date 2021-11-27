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

namespace Som.Interpreter;
using Som.VM;
using Som.VMObject;

/**
 * @formatter:off
 * Frame layout:
 *
 * +-----------------+
 * | Arguments       | 0
 * +-----------------+
 * | Local Variables | <-- localOffset
 * +-----------------+
 * | Stack           | <-- stackPointer
 * | ...             |
 * +-----------------+
 * @formatter:on
 */
public class Frame
{
    public Frame(SObject nilObject, Frame previousFrame,Frame context, SMethod method, long stackElements)
    {
        this.previousFrame = previousFrame;
        this.context = context;
        this.method = method;
        this.stack = new SAbstractObject[(int)stackElements];

        for (int i = 0; i < stackElements; i++) stack[i] = nilObject;
        // Reset the stack pointer and the bytecode index
        resetStackPointer();
        bytecodeIndex = 0;
    }

    public Frame getPreviousFrame() => previousFrame;

    public void clearPreviousFrame() => previousFrame = null;

    public bool hasPreviousFrame() => previousFrame != null;

    public bool isBootstrapFrame() => !hasPreviousFrame();

    public Frame getContext() => context;

    public bool hasContext() => context != null;

    public Frame getContext(int level)
    {
        // Get the context frame at the given level
        var frame = this;

        // Iterate through the context chain until the given level is reached
        while (level > 0)
        {
            // Get the context of the current frame
            frame = frame.getContext();

            // Go to the next level
            level--;
        }

        // Return the found context
        return frame;
    }

    public Frame getOuterContext()
    {
        // Compute the outer context of this frame
        var frame = this;

        // Iterate through the context chain until null is reached
        while (frame.hasContext())
        {
            frame = frame.getContext();
        }

        // Return the outer context
        return frame;
    }

    public SMethod getMethod() => method;

    public SAbstractObject pop()
    {
        // Pop an object from the expression stack and return it
        int sp = stackPointer;
        stackPointer -= 1;
        return stack[sp];
    }

    public void push(SAbstractObject value)
    {
        // Push an object onto the expression stack
        int sp = stackPointer + 1;
        stack[sp] = value;
        stackPointer = sp;
    }

    public void resetStackPointer()
    {
        // arguments are stored in front of local variables
        localOffset = getMethod().getNumberOfArguments();

        // Set the stack pointer to its initial value thereby clearing the stack
        stackPointer = localOffset + getMethod().getNumberOfLocals() - 1;
    }

    public int getBytecodeIndex() =>
        // Get the current bytecode index for this frame
        bytecodeIndex;

    public void setBytecodeIndex(int value) =>
        // Set the current bytecode index for this frame
        bytecodeIndex = value;

    public SAbstractObject getStackElement(int index) =>
        // Get the stack element with the given index
        // (an index of zero yields the top element)
        stack[stackPointer - index];

    public void setStackElement(int index, SAbstractObject value) =>
        // Set the stack element with the given index to the given value
        // (an index of zero yields the top element)
        stack[stackPointer - index] = value;

    private SAbstractObject getLocal(int index) 
        => stack[localOffset + index];

    private void setLocal(int index, SAbstractObject value) 
        => stack[localOffset + index] = value;

    public SAbstractObject getLocal(int index, int contextLevel) =>
        // Get the local with the given index in the given context
        getContext(contextLevel).getLocal(index);

    public void setLocal(int index, int contextLevel, SAbstractObject value) =>
        // Set the local with the given index in the given context to the given
        // value
        getContext(contextLevel).setLocal(index, value);

    public SAbstractObject getArgument(int index, int contextLevel) =>
        // Get the context
        // Get the argument with the given index
        getContext(contextLevel).stack[index];

    public void setArgument(int index, int contextLevel, SAbstractObject value) =>
        // Get the context
        // Set the argument with the given index to the given value
        getContext(contextLevel).stack[index] = value;

    public void copyArgumentsFrom(Frame frame)
    {
        // copy arguments from frame:
        // - arguments are at the top of the stack of frame.
        // - copy them into the argument area of the current frame
        int numArgs = getMethod().getNumberOfArguments();
        for (int i = 0; i < numArgs; ++i)
        {
            stack[i] = frame.getStackElement(numArgs - 1 - i);
        }
    }

    public void printStackTrace()
    {
        // Print a stack trace starting in this frame
        if (hasPreviousFrame()) getPreviousFrame().printStackTrace();

        var className = getMethod().getHolder().getName().getEmbeddedString();
        var methodName = getMethod().getSignature().getEmbeddedString();
        Universe.println(className + ">>#" + methodName + " @bi: " + bytecodeIndex);
    }

    // Private variables holding the stack pointer and the bytecode index
    protected int stackPointer;
    protected int bytecodeIndex;
    // the offset at which local variables start
    protected int localOffset;

    protected SMethod method;
    protected Frame context;
    protected Frame previousFrame;
    protected SAbstractObject[] stack;
}
