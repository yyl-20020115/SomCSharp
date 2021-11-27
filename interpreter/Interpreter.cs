﻿/**
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

namespace Som.Interpreter;
using Som.VM;
using Som.VMObject;
using static Som.Interpreter.Bytecodes;

public class Interpreter
{

    protected Universe universe;

    public Interpreter(Universe universe)
    {
        this.universe = universe;
    }

    private void doDup() =>
        // Handle the DUP bytecode
        getFrame().push(getFrame().getStackElement(0));

    private void doPushLocal(int bytecodeIndex) =>
        // Handle the PUSH LOCAL bytecode
        getFrame().push(
            getFrame().getLocal(getMethod().getBytecode(bytecodeIndex + 1),
                getMethod().getBytecode(bytecodeIndex + 2)));

    private void doPushArgument(int bytecodeIndex) =>
        // Handle the PUSH ARGUMENT bytecode
        getFrame().push(
            getFrame().getArgument(getMethod().getBytecode(bytecodeIndex + 1),
                getMethod().getBytecode(bytecodeIndex + 2)));

    private void doPushField(int bytecodeIndex)
    {
        // Handle the PUSH FIELD bytecode
        int fieldIndex = getMethod().getBytecode(bytecodeIndex + 1);

        // Push the field with the computed index onto the stack
        getFrame().push(((SObject)getSelf()).getField(fieldIndex));
    }

    private void doPushBlock(int bytecodeIndex)
    {
        // Handle the PUSH BLOCK bytecode
        var blockMethod = (SMethod)getMethod().getConstant(bytecodeIndex);

        // Push a new block with the current getFrame() as context onto the
        // stack
        getFrame().push(
            universe.newBlock(blockMethod, getFrame(),
                blockMethod.getNumberOfArguments()));
    }

    private void doPushConstant(int bytecodeIndex) =>
        // Handle the PUSH CONSTANT bytecode
        getFrame().push(getMethod().getConstant(bytecodeIndex));

    private void doPushGlobal(int bytecodeIndex)
    {
        // Handle the PUSH GLOBAL bytecode
        var globalName = (SSymbol)getMethod().getConstant(bytecodeIndex);

        // Get the global from the universe
        var global = universe.getGlobal(globalName);

        if (global != null)
        {
            // Push the global onto the stack
            getFrame().push(global);
        }
        else
        {
            // Send 'unknownGlobal:' to self
            getSelf().sendUnknownGlobal(globalName, universe, this);
        }
    }

    private void doPop() =>
        // Handle the POP bytecode
        getFrame().pop();

    private void doPopLocal(int bytecodeIndex) =>
        // Handle the POP LOCAL bytecode
        getFrame().setLocal(getMethod().getBytecode(bytecodeIndex + 1),
            getMethod().getBytecode(bytecodeIndex + 2), getFrame().pop());

    private void doPopArgument(int bytecodeIndex) =>
        // Handle the POP ARGUMENT bytecode
        getFrame().setArgument(getMethod().getBytecode(bytecodeIndex + 1),
            getMethod().getBytecode(bytecodeIndex + 2), getFrame().pop());

    private void doPopField(int bytecodeIndex)
    {
        // Handle the POP FIELD bytecode
        int fieldIndex = getMethod().getBytecode(bytecodeIndex + 1);

        // Set the field with the computed index to the value popped from the stack
        ((SObject)getSelf()).setField(fieldIndex, getFrame().pop());
    }

    private void doSuperSend(int bytecodeIndex)
    {
        // Handle the SUPER SEND bytecode
        var signature = (SSymbol)getMethod().getConstant(bytecodeIndex);

        // Send the message
        // Lookup the invokable with the given signature
        var holderSuper = (SClass)getMethod().getHolder().getSuperClass();
        var invokable = holderSuper.lookupInvokable(signature);

        activateOrDnu(signature, invokable);
    }

    private void doReturnLocal()
    {
        // Handle the RETURN LOCAL bytecode
        var result = getFrame().pop();
        // Pop the top frame and push the result
        popFrameAndPushResult(result);
    }

    private void doReturnNonLocal()
    {
        // Handle the RETURN NON LOCAL bytecode
        var result = getFrame().pop();
        // Compute the context for the non-local return
        var context = getFrame().getOuterContext();
        // Make sure the block context is still on the stack
        if (!context.hasPreviousFrame())
        {
            // Try to recover by sending 'escapedBlock:' to the sending object
            // this can get a bit nasty when using nested blocks. In this case
            // the "sender" will be the surrounding block and not the object
            // that actually sent the 'value' message.
            var block = (SBlock)getFrame().getArgument(0, 0);
            var sender =
                getFrame().getPreviousFrame().getOuterContext().getArgument(0, 0);

            // pop the frame of the currently executing block...
            popFrame();

            // pop old arguments from stack
            SMethod method = getFrame().getMethod();
            int numArgs = method.getNumberOfArguments();
            for (int i = 0; i < numArgs; i += 1)
            {
                getFrame().pop();
            }

            // ... and execute the escapedBlock message instead
            sender.sendEscapedBlock(block, universe, this);
            return;
        }

        // Unwind the frames
        while (getFrame() != context)
        {
            popFrame();
        }

        // Pop the top frame and push the result
        popFrameAndPushResult(result);
    }

    private void doSend(int bytecodeIndex)
    {
        // Handle the SEND bytecode
        var signature = (SSymbol)getMethod().getConstant(bytecodeIndex);

        // Get the number of arguments from the signature
        int numberOfArguments = signature.getNumberOfSignatureArguments();

        // Get the receiver from the stack
        var receiver = getFrame().getStackElement(numberOfArguments - 1);

        // Send the message
        send(signature, receiver.getSOMClass(universe), bytecodeIndex);
    }

    public SAbstractObject start()
    {
        // Iterate through the bytecodes
        while (true)
        {

            // Get the current bytecode index
            int bytecodeIndex = getFrame().getBytecodeIndex();

            // Get the current bytecode
            byte bytecode = getMethod().getBytecode(bytecodeIndex);

            // Get the length of the current bytecode
            int bytecodeLength = getBytecodeLength(bytecode);

            // Compute the next bytecode index
            int nextBytecodeIndex = bytecodeIndex + bytecodeLength;

            // Update the bytecode index of the frame
            getFrame().setBytecodeIndex(nextBytecodeIndex);

            // Handle the current bytecode
            switch (bytecode)
            {

                case HALT:
                    {
                        // Handle the HALT bytecode
                        return getFrame().getStackElement(0);
                    }

                case DUP:
                    {
                        doDup();
                        break;
                    }

                case PUSH_LOCAL:
                    {
                        doPushLocal(bytecodeIndex);
                        break;
                    }

                case PUSH_ARGUMENT:
                    {
                        doPushArgument(bytecodeIndex);
                        break;
                    }

                case PUSH_FIELD:
                    {
                        doPushField(bytecodeIndex);
                        break;
                    }

                case PUSH_BLOCK:
                    {
                        doPushBlock(bytecodeIndex);
                        break;
                    }

                case PUSH_CONSTANT:
                    {
                        doPushConstant(bytecodeIndex);
                        break;
                    }

                case PUSH_GLOBAL:
                    {
                        doPushGlobal(bytecodeIndex);
                        break;
                    }

                case POP:
                    {
                        doPop();
                        break;
                    }

                case POP_LOCAL:
                    {
                        doPopLocal(bytecodeIndex);
                        break;
                    }

                case POP_ARGUMENT:
                    {
                        doPopArgument(bytecodeIndex);
                        break;
                    }

                case POP_FIELD:
                    {
                        doPopField(bytecodeIndex);
                        break;
                    }

                case SEND:
                    {
                        doSend(bytecodeIndex);
                        break;
                    }

                case SUPER_SEND:
                    {
                        doSuperSend(bytecodeIndex);
                        break;
                    }

                case RETURN_LOCAL:
                    {
                        doReturnLocal();
                        break;
                    }

                case RETURN_NON_LOCAL:
                    {
                        doReturnNonLocal();
                        break;
                    }

                default:
                    Universe.errorPrintln("Nasty bug in interpreter");
                    break;
            }
        }
    }

    public Frame pushNewFrame(SMethod method, Frame contextFrame)
    {
        // Allocate a new frame and make it the current one
        frame = universe.newFrame(frame, method, contextFrame);

        // Return the freshly allocated and pushed frame
        return frame;
    }

    public Frame pushNewFrame(SMethod method) => pushNewFrame(method, null);

    public Frame getFrame() =>
        // Get the frame from the interpreter
        frame;

    public SMethod getMethod() =>
        // Get the method from the interpreter
        getFrame().getMethod();

    public SAbstractObject getSelf() =>
        // Get the self object from the interpreter
        getFrame().getOuterContext().getArgument(0, 0);

    private void send(SSymbol selector, SClass receiverClass,
         int bytecodeIndex)
    {
        // First try the inline cache
        SInvokable invokable;

        var m = getMethod();
        var cachedClass = m.getInlineCacheClass(bytecodeIndex);
        if (cachedClass == receiverClass)
        {
            invokable = m.getInlineCacheInvokable(bytecodeIndex);
        }
        else
        {
            if (cachedClass == null)
            {
                // Lookup the invokable with the given signature
                invokable = receiverClass.lookupInvokable(selector);
                m.setInlineCache(bytecodeIndex, receiverClass, invokable);
            }
            else
            {
                // the bytecode index after the send is used by the selector constant, and can be used
                // safely as another cache item
                cachedClass = m.getInlineCacheClass(bytecodeIndex + 1);
                if (cachedClass == receiverClass)
                {
                    invokable = m.getInlineCacheInvokable(bytecodeIndex + 1);
                }
                else
                {
                    invokable = receiverClass.lookupInvokable(selector);
                    if (cachedClass == null)
                    {
                        m.setInlineCache(bytecodeIndex + 1, receiverClass, invokable);
                    }
                }
            }
        }

        activateOrDnu(selector, invokable);
    }

    public void activateOrDnu(SSymbol selector, SInvokable invokable)
    {
        if (invokable != null)
        {
            // Invoke the invokable in the current frame
            invokable.invoke(getFrame(), this);
        }
        else
        {
            int numberOfArguments = selector.getNumberOfSignatureArguments();

            // Compute the receiver
            var receiver = frame.getStackElement(numberOfArguments - 1);

            receiver.sendDoesNotUnderstand(selector, universe, this);
        }
    }

    private Frame popFrame()
    {
        // Save a reference to the top frame
        var result = frame;

        // Pop the top frame from the frame stack
        frame = frame.getPreviousFrame();

        // Destroy the previous pointer on the old top frame
        result.clearPreviousFrame();

        // Return the popped frame
        return result;
    }

    private void popFrameAndPushResult(SAbstractObject result)
    {
        // Pop the top frame from the interpreter frame stack and compute the
        // number of arguments
        int numberOfArguments = popFrame().getMethod().getNumberOfArguments();

        // Pop the arguments
        for (int i = 0; i < numberOfArguments; i++)
        {
            getFrame().pop();
        }

        // Push the result
        getFrame().push(result);
    }

    protected Frame frame;
}
