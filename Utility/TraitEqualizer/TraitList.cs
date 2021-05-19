using System;
using System.Collections.Generic;
using System.Linq;



namespace ModPack
{
    public class TraitList<T>
    {
        // Publics
        public void Add(TraitElement<T> element)
        {
            _elements.Add(element);
            foreach (var trait in element.Traits)
                _countsByTrait[trait]++;
        }
        public int TraitCount(Trait<T> trait)
        => _countsByTrait[trait];
        public IEnumerable<TraitElement<T>> Elements
        {
            get
            {
                foreach (var element in _elements)
                    yield return element;
            }
        }

        // Privates
        public List<TraitElement<T>> _elements;
        private Dictionary<Trait<T>, int> _countsByTrait;

        // Constructors
        public TraitList(Trait<T>[] traits)
        {
            _elements = new List<TraitElement<T>>();
            _countsByTrait = new Dictionary<Trait<T>, int>();
            foreach (var trait in traits)
                _countsByTrait.Add(trait, 0);
        }
    }
}
