using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Tenant : IEquatable<Tenant>
{
    public string Name;

    //Equality checking
    public bool Equals(Tenant other)
    {
        return (other != null && other.Name == Name);
    }
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}