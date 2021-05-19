using System;
using System.Collections.Generic;
using System.Linq;


namespace ModPack
{
    public class TraitEqualizer<T>
    {
        // Publics
        public void Add(T value)
        {
            // Get potential lists pool
            IEnumerable<Trait<T>> activeTraits = GetTraits(value);
            var potentials = Utility.Intersect(GetMinListsByTrait(activeTraits));
            if (potentials.Count == 0)
                potentials = GetListsWithMostMinTraits(activeTraits);

            // Choose a list and add element
            TraitList<T> chosenList = potentials.Random();
            chosenList.Add(new TraitElement<T>(value, activeTraits));

            // Update minLists
            foreach (var trait in activeTraits)
                if (_minListsByTrait[trait].Remove(chosenList) && _minListsByTrait[trait].Count == 0)
                    _minListsByTrait[trait] = GetMinLists(trait, chosenList.TraitCount(trait));
        }
        public IEnumerable<IEnumerable<T>> Results
        {
            get
            {
                foreach (var list in _lists)
                    yield return list.Elements.Select(element => element.Value);
            }
        }
        public IEnumerable<Trait<T>> Traits
        {
            get
            {
                foreach (var trait in _traits)
                    yield return trait;
            }
        }

        // Privates
        private TraitList<T>[] _lists;
        private Trait<T>[] _traits;
        private Dictionary<Trait<T>, List<TraitList<T>>> _minListsByTrait;
        private IEnumerable<Trait<T>> GetTraits(T element)
        {
            foreach (var trait in _traits)
                if (trait.Test(element))
                    yield return trait;
        }
        private List<TraitList<T>> GetMinLists(Trait<T> trait, int minThrehsold)
        => _lists.Where(list => list.TraitCount(trait) <= minThrehsold).ToList();
        private IEnumerable<List<TraitList<T>>> GetMinListsByTrait(IEnumerable<Trait<T>> traits)
        {
            foreach (var trait in traits)
                yield return _minListsByTrait[trait];
        }
        private List<TraitList<T>> GetListsWithMostMinTraits(IEnumerable<Trait<T>> traits)
        {
            var resultLists = new List<TraitList<T>>();
            int maxCount = 0;
            foreach (var list in _lists)
            {
                int count = 0;
                foreach (var trait in traits)
                    if (_minListsByTrait[trait].Contains(list))
                        count++;

                if (count > maxCount)
                {
                    resultLists.Clear();
                    maxCount = count;
                }
                if (count == maxCount)
                    resultLists.Add(list);
            }
            return resultLists;
        }

        // Constructors
        public TraitEqualizer(int listsCount, Trait<T>[] traits)
        {
            _traits = traits;
            _lists = new TraitList<T>[listsCount];
            for (int i = 0; i < _lists.Length; i++)
                _lists[i] = new TraitList<T>(_traits);

            _minListsByTrait = new Dictionary<Trait<T>, List<TraitList<T>>>();
            foreach (var trait in _traits)
                _minListsByTrait[trait] = _lists.ToList();
        }
    }
}


// Log.Line("Optimal list not found!", ConsoleColor.Red);
/* Log
PrintStats(_traits, newElement.Traits, listsPool);
Log.Line();
Log.WaitForInput();
*/