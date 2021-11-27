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

namespace Som.VM;
using Som.Interpreter;
using Som.VMObject;

public class Shell
{

    protected Universe    universe;
    protected Interpreter interpreter;
    protected SMethod bootstrapMethod;

    public Shell(Universe universe, Interpreter interpreter)
    {
        this.universe = universe;
        this.interpreter = interpreter;
    }


    public void setBootstrapMethod(SMethod method)
    {
        bootstrapMethod = method;
    }

    public SAbstractObject start()
    {
        TextReader reader;
        string stmt;
        int counter;
        int bytecodeIndex;
        SClass myClass;
        SAbstractObject myObject;
        SAbstractObject it;
        Frame currentFrame;

        counter = 0;
        reader= Console.In;
        it = universe.nilObject;

        Universe.println("SOM Shell. Type \"quit\" to exit.\n");

        // Create a fake bootstrap frame
        currentFrame = interpreter.pushNewFrame(bootstrapMethod);

        // Remember the first bytecode index, e.g. index of the HALT instruction
        bytecodeIndex = currentFrame.getBytecodeIndex();

        while (true)
        {
            try
            {
                Universe.print("---> ");

                // Read a statement from the keyboard
                stmt = reader.ReadLine();
                if (stmt==("quit")) return it;

                // Generate a temporary class with a run method
                stmt = "Shell_Class_" + counter++ + " = ( run: it = ( | tmp | tmp := ("
                    + stmt + " ). 'it = ' print. ^tmp println ) )";

                // Compile and load the newly generated class
                myClass = universe.loadShellClass(stmt);

                // If success
                if (myClass != null)
                {
                    currentFrame = interpreter.getFrame();

                    // Go back, so we will evaluate the bootstrap frames halt
                    // instruction again
                    currentFrame.setBytecodeIndex(bytecodeIndex);

                    // Create and push a new instance of our class on the stack
                    myObject = universe.newInstance(myClass);
                    currentFrame.push(myObject);

                    // Push the old value of "it" on the stack
                    currentFrame.push(it);

                    // Lookup the run: method
                    SInvokable initialize = myClass.lookupInvokable(
                        universe.symbolFor("run:"));

                    // Invoke the run method
                    initialize.invoke(currentFrame, interpreter);

                    // Start the interpreter
                    interpreter.start();

                    // Save the result of the run method
                    it = currentFrame.pop();
                }
            }
            catch (Exception e)
            {
                Universe.errorPrintln("Caught exception: " + e.Message);
                Universe.errorPrintln("" + interpreter.getFrame().getPreviousFrame());
            }
        }
    }
}
