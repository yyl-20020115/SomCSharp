﻿/**
 * Copyright (c) 2013 Stefan Marr,   stefan.marr@vub.ac.be
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

namespace Som.Primitives;
using Som.VM;
using Som.VMObject;

public abstract class Primitives
{

    protected Universe universe;

    public Primitives(Universe universe) => this.universe = universe;

    public void InstallPrimitivesIn(SClass value)
    {
        // Save a reference to the holder class
        holder = value;

        // Install the primitives from this primitives class
        this.InstallPrimitives();
    }

    public abstract void InstallPrimitives();

    protected void InstallInstancePrimitive(SPrimitive primitive) => this.installInstancePrimitive(primitive, false);

    protected void installInstancePrimitive(SPrimitive primitive, bool suppressWarning) =>
        // Install the given primitive as an instance primitive in the holder
        // class
        this.holder.AddInstancePrimitive(primitive, suppressWarning);

    protected void InstallClassPrimitive(SPrimitive primitive) =>
        // Install the given primitive as an instance primitive in the class of
        // the holder class
        this.holder.SOMClass.AddInstancePrimitive(primitive);

    protected SClass holder;
}
