using System;
using System.Collections.Generic;
using System.Linq;



namespace ModPack
{
    public class TraitElement<T>
    {
        // Publics
        public T Value
        { get; private set; }
        public IEnumerable<Trait<T>> Traits
        { get; private set; }

        // Constructors
        public TraitElement(T value, IEnumerable<Trait<T>> traits)
        {
            Value = value;
            Traits = traits;
        }
    }
}
