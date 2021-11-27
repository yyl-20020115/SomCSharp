/**
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
namespace Som.Compiler;
using Som.VM;
using Som.VMObject;

public class ClassGenerationContext
{
    protected Universe universe;
    protected SSymbol name;
    protected SSymbol superName;
    protected bool classSide;
    protected List<SSymbol> instanceFields = new ();
    protected List<SInvokable> instanceMethods = new ();
    protected List<SSymbol> classFields = new ();
    protected List<SInvokable> classMethods = new ();
    public SSymbol Name { get => name; set => this.name = value; }
    public ClassGenerationContext(Universe universe) => this.universe = universe;
    public void setSuperName(SSymbol superName) => this.superName = superName;
    public void setInstanceFieldsOfSuper(SArray fieldNames)
    {
        int numFields = fieldNames.getNumberOfIndexableFields();
        for (int i = 0; i < numFields; i++)
            instanceFields.Add(fieldNames.getIndexableField(i) as SSymbol);
    }
    public void setClassFieldsOfSuper(SArray fieldNames)
    {
        int numFields = fieldNames.getNumberOfIndexableFields();
        for (int i = 0; i < numFields; i++)
            classFields.Add(fieldNames.getIndexableField(i) as SSymbol);
    }
    public void addMethod(SInvokable meth)
    {
        if (classSide)
            classMethods.Add(meth);
        else
            instanceMethods.Add(meth);
    }
    public void startClassSide() => classSide = true;
    public void addField(SSymbol field)
    {
        if (classSide)
            classFields.Add(field);
        else
            instanceFields.Add(field);
    }
    public bool hasField(SSymbol field) => (isClassSide() ? classFields : instanceFields).Contains(field);
    public byte getFieldIndex(SSymbol field) => isClassSide() ? (byte)classFields.IndexOf(field) : (byte)instanceFields.IndexOf(field);
    public bool isClassSide() => classSide;
    public SClass assemble()
    {
        // build class class name
        var ccname = name.getEmbeddedString() + " class";
        // Load the super class
        var superClass = universe.loadClass(superName);
        // Allocate the class of the resulting class
        var resultClass = universe.newClass(universe.metaclassClass);
        // Initialize the class of the resulting class
        resultClass.setInstanceFields(universe.newArray(classFields));
        resultClass.setInstanceInvokables(universe.newArray(classMethods));
        resultClass.setName(universe.symbolFor(ccname));
        var superMClass = superClass.getSOMClass();
        resultClass.setSuperClass(superMClass);
        // Allocate the resulting class
        var result = universe.newClass(resultClass);
        // Initialize the resulting class
        result.setName(name);
        result.setSuperClass(superClass);
        result.setInstanceFields(universe.newArray(instanceFields));
        result.setInstanceInvokables(universe.newArray(instanceMethods));
        return result;
    }
    public SClass assembleSystemClass(SClass systemClass)
    {
        systemClass.setInstanceInvokables(universe.newArray(instanceMethods));
        systemClass.setInstanceFields(universe.newArray(instanceFields));
        var superMClass = systemClass.getSOMClass();
        superMClass.setInstanceInvokables(universe.newArray(classMethods));
        superMClass.setInstanceFields(universe.newArray(classFields));
        return systemClass;
    }
}
