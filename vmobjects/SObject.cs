namespace Som.VMObject;
using Som.VM;

public class SObject : SAbstractObject
{

    public SObject(SObject nilObject)
    {
        fields = new SAbstractObject[getDefaultNumberOfFields()];
        // Clear each and every field by putting nil into them
        for (int i = 0; i < getNumberOfFields(); i++)
            setField(i, nilObject);
    }

    public SObject(int numberOfFields, SObject nilObject)
    {
        fields = new SAbstractObject[numberOfFields];
        // Clear each and every field by putting nil into them
        for (int i = 0; i < getNumberOfFields(); i++)
            setField(i, nilObject);
    }

    public virtual SClass getSOMClass() => clazz;

    public void setClass(SClass value) =>
        // Set the class of this object by writing to the field with class index
        clazz = value;

    public SSymbol getFieldName(int index) =>
        // Get the name of the field with the given index
        getSOMClass().getInstanceFieldName(index);

    public int getFieldIndex(SSymbol name) =>
        // Get the index for the field with the given name
        getSOMClass().lookupFieldIndex(name);

    public int getNumberOfFields() =>
        // Get the number of fields in this object
        fields.Length;

    public int getDefaultNumberOfFields() =>
        // Return the default number of fields in an object
        numberOfObjectFields;

    public SAbstractObject getField(long index) =>
        // Get the field with the given index
        fields[(int)index];

    public void setField(long index, SAbstractObject value) =>
        // Set the field with the given index to the given value
        fields[(int)index] = value;

    public override SClass getSOMClass(Universe universe) => clazz;

    public override string ToString()
    {
        if (clazz.getName().getEmbeddedString() == ("SObject"))
        {
            if (fields[1] is SObject)
            {
                var somClazz = (SObject)fields[1];
                var nameSymbolObj = (SObject)somClazz.fields[4];
                var nameString = (SString)nameSymbolObj.fields[0];
                return "SomSom: a " + nameString.getEmbeddedString();
            }
        }
        return "a " + getSOMClass(Universe.Current()).getName().getEmbeddedString();
    }

    // Private array of fields
    protected SAbstractObject[] fields;
    protected SClass clazz;

    // Static field indices and number of object fields
    public static int numberOfObjectFields = 0;
}
