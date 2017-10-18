using System;

namespace ScatLib
{
    [Flags]
    public enum NearFieldFlag
    {
        HasElectricField = 1,
        HasMagneticField,
        HasRefractiveIndexMap
    }

    public enum NearFieldType
    {
        Incident,
        Scattered
    }
}