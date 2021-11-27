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
using Som.VM;

public class SArray : SAbstractObject
{
    public SArray(SObject nilObject, long numElements)
    {
        indexableFields = new SAbstractObject[(int)numElements];

        // Clear each and every field by putting nil into them
        for (int i = 0; i < getNumberOfIndexableFields(); i++)
        {
            setIndexableField(i, nilObject);
        }
    }

    public SAbstractObject getIndexableField(long index) => indexableFields[(int)index];

    public void setIndexableField(long index, SAbstractObject value) => indexableFields[(int)index] = value;

    public int getNumberOfIndexableFields() => indexableFields.Length;

    public SArray copyAndExtendWith(SAbstractObject value, Universe universe)
    {
        // Allocate a new array which has one indexable field more than this
        // array
        var result = universe.newArray(getNumberOfIndexableFields() + 1);

        // Copy the indexable fields from this array to the new array
        copyIndexableFieldsTo(result);

        // Insert the given object as the last indexable field in the new array
        result.setIndexableField(getNumberOfIndexableFields(), value);

        return result;
    }

    protected void copyIndexableFieldsTo(SArray destination)
    {
        // Copy all indexable fields from this array to the destination array
        for (int i = 0; i < getNumberOfIndexableFields(); i++)
        {
            destination.setIndexableField(i, getIndexableField(i));
        }
    }

    public override SClass getSOMClass(Universe universe) => universe.arrayClass;

    // Private array of indexable fields
    private SAbstractObject[] indexableFields;
}
