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

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;

public class SourcecodeCompiler
{
    protected Parser parser;
    public static SClass compileClass(string path, string file,
        SClass systemClass, Universe universe) 
        => new SourcecodeCompiler().compile(path, file, systemClass, universe);
    public static SClass compileClass(string stmt, SClass systemClass, Universe universe) 
        => new SourcecodeCompiler().compileClassString(stmt, systemClass, universe);
    private SClass compile(string path, string file,SClass systemClass, Universe universe)
    {
        var fname = path + Universe.fileSeparator + file + ".som";
        this.parser = new Parser(new StreamReader(fname), universe, fname);
        var result = compile(systemClass);
        var cname = result.getName();
        var cnameC = cname.getEmbeddedString();
        if (file != cnameC)
            throw new ProgramDefinitionError("File name " + fname
                + " does not match class name (" + cnameC + ") in it.");
        return result;
    }

    private SClass compileClassString(string stream,SClass systemClass, Universe universe)
    {
        this.parser = new(new StringReader(stream), universe, "$string$");
        return this.compile(systemClass);
    }

    private SClass compile(SClass systemClass)
    {
        var cgc = this.parser.classdef();
        return systemClass == null ? cgc.assemble() : cgc.assembleSystemClass(systemClass);
    }
}
