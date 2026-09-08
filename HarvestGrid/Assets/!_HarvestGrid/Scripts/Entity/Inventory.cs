using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory : ICloneable<Inventory>
{

    public Inventory(Inventory original)
    {

    }
    public Inventory Clone()
    {
        throw new System.NotImplementedException();
    }
}