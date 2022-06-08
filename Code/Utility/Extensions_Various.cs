namespace Vheos.Mods.Outward;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Extensions_Various
{
    // Game
    public static bool IsPlayer(this Character character)
    => character.PlayerStats != null;
    public static bool IsAlly(this Character character)
    => character.Faction == Character.Factions.Player;
    public static bool IsEnemy(this Character character)
    => character.IsEnemyOf(Character.Factions.Player);
    public static bool IsEnemyOf(this Character character, Character.Factions faction)
    => character.TargetingSystem.IsTargetable(faction);
    public static bool IsOwnerOf(this Character character, Item item)
    => character == item.OwnerCharacter;
    public static bool IsOwnerOf(this Character character, MeleeHitDetector hitDetector)
    => character == hitDetector.OwnerCharacter;
    public static bool HasStatusEffect(this Character character, StatusEffect statusEffect)
    => character.StatusEffectMngr.HasStatusEffect(statusEffect.IdentifierName);
    public static bool IsIngestible(this Item item)
    => Prefabs.IngestiblesByID.ContainsKey(item.ItemID);
    public static bool IsEatable(this Item item)
    => item.IsUsable && item.ActivateEffectAnimType == Character.SpellCastType.Eat;
    public static bool IsDrinkable(this Item item)
    => item.IsUsable && (item.ActivateEffectAnimType == Character.SpellCastType.DrinkWater || item.ActivateEffectAnimType == Character.SpellCastType.Potion);
    public static bool HasAnyPurgeableNegativeStatusEffect(this Character character)
    {
        foreach (var statusEffect in character.StatusEffectMngr.Statuses)
            if (statusEffect.IsMalusEffect && statusEffect.Purgeable)
                return true;
        return false;
    }
    public static bool IsBurning(this Character character)
    => character.StatusEffectMngr.HasStatusEffect("Burning");
    public static Disease GetDiseaseOfFamily(this Character character, StatusEffectFamily statusEffectFamily)
    {
        foreach (var statusEffect in character.StatusEffectMngr.Statuses)
            if (statusEffect.TryAs(out Disease disease) && statusEffect.EffectFamily.UID == statusEffectFamily.UID)
                return disease;
        return null;
    }
    public static bool HasLearnedRecipe(this Character character, Recipe recipe)
    => character.Inventory.RecipeKnowledge.IsRecipeLearned(recipe.UID);
    public static void LearnRecipe(this Character character, Recipe recipe)
    => character.Inventory.RecipeKnowledge.LearnRecipe(recipe);

    // Item effects
    public static T GetEffect<T>(this Item item) where T : Effect
    {
        foreach (Transform child in item.transform)
        {
            T effect = child.GetComponent<T>();
            if (effect != null)
                return effect;
        }
        return null;
    }
    public static T[] GetEffects<T>(this Item item) where T : Effect
    => item.GetComponentsInChildren<T>();
    public static Effect[] GetEffects(this Item item)
    => item.GetEffects<Effect>();
    public static T AddEffect<T>(this Item item) where T : Effect
    {
        GameObject effectsHolder = item.FindChild("Effects");
        if (effectsHolder == null)
        {
            effectsHolder = new GameObject("Effects");
            effectsHolder.BecomeChildOf(item);
        }
        T effect = effectsHolder.AddComponent<T>();
        UnityEngine.Object.DontDestroyOnLoad(item);
        return effect;
    }
    public static float GetValue(this Effect effect)
        => effect switch
        {
            AffectHealth t => t.AffectQuantity,
            AffectStamina t => t.AffectQuantity,
            AffectMana t => t.Value,
            AffectBurntHealth t => t.AffectQuantity,
            AffectBurntStamina t => t.AffectQuantity,
            AffectBurntMana t => t.AffectQuantity,
            AffectFood t => t.m_affectQuantity,
            AffectDrink t => t.m_affectQuantity,
            AffectFatigue t => t.m_affectQuantity,
            AffectCorruption t => t.AffectQuantity,
            AffectStat t => t.Value,
            _ => default,
        };
    public static void SetValue(this Effect effect, float value)
    {
        switch (effect)
        {
            case AffectHealth t: t.AffectQuantity = value; break;
            case AffectStamina t: t.AffectQuantity = value; break;
            case AffectMana t: t.Value = value; break;
            case AffectBurntHealth t: t.AffectQuantity = value; break;
            case AffectBurntStamina t: t.AffectQuantity = value; break;
            case AffectBurntMana t: t.AffectQuantity = value; break;
            case AffectFood t: t.m_affectQuantity = value; break;
            case AffectDrink t: t.m_affectQuantity = value; break;
            case AffectFatigue t: t.m_affectQuantity = value; break;
            case AffectCorruption t: t.AffectQuantity = value; break;
            case AffectStat t: t.Value = value; break;
        }
    }
    public static void RemoveAllEffects(this Item item)
    {
        foreach (Transform child in item.transform)
            child.GetComponents<Effect>().Destroy();
    }

    // StatusEffects effects
    public static (StatusEffect, string[]) GetStatusEffectAndValuesOfEffect<T>(this Item item) where T : Effect
    {
        foreach (var addStatusEffect in item.GetEffects<AddStatusEffect>())
        {
            StatusEffect statusEffect = addStatusEffect.Status;
            string[] values = statusEffect.GetValuesOfEffect<T>();
            if (values != null)
                return (statusEffect, values);
        }
        return default;
    }
    public static string[] GetValuesOfEffect<T>(this StatusEffect statusEffect) where T : Effect
    {
        List<Effect> effects = statusEffect.StatusData.EffectSignature.Effects;
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] is T)
                return statusEffect.StatusData.EffectsData[i].Data;
        return null;
    }
    public static Dictionary<Effect, string[]> GetValuesByEffect(this StatusEffect statusEffect)
    {
        // Cache
        Dictionary<Effect, string[]> valuesByEffect = new();
        EffectSignature effectSignature = statusEffect.StatusData.EffectSignature;
        if (effectSignature == null)
            return valuesByEffect;

        List<Effect> effects = effectSignature.Effects;
        StatusData.EffectData[] effectDatas = statusEffect.StatusData.EffectsData;

        // Add        
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null)
                continue;

            string[] values = new string[0];
            if (effectDatas.IsValid(i))
                values = effectDatas[i].Data;

            valuesByEffect.Add(effects[i], values);
        }
        return valuesByEffect;
    }
    public static void RemoveAllEffects(this StatusEffect statusEffect)
    => statusEffect.StatusData.EffectSignature.GetComponentsInChildren<Effect>().Destroy();
    public static T AddEffect<T>(this StatusEffect statusEffect) where T : Effect
    {
        EffectSignature effectSignature = statusEffect.StatusData.EffectSignature;
        GameObject effectsHolder = effectSignature.FindChild("Effects");
        if (effectsHolder == null)
        {
            effectsHolder = new GameObject("Effects");
            effectsHolder.BecomeChildOf(effectSignature);
        }
        T effect = effectsHolder.AddComponent<T>();
        UnityEngine.Object.DontDestroyOnLoad(statusEffect);
        return effect;
    }
    public static bool HasEffectsAndDatas(this StatusEffect statusEffect)
    {
        EffectSignature effectSignature = statusEffect.StatusData.EffectSignature;
        if (effectSignature == null)
            return false;

        List<Effect> effects = effectSignature.Effects;
        if (effects.IsNullOrEmpty())
            return false;

        StatusData.EffectData[] effectDatas = statusEffect.StatusData.EffectsData;
        return !effectDatas.IsNullOrEmpty();
    }
    public static List<Effect> GetEffects(this StatusEffect statusEffect)
    => statusEffect.StatusData.EffectSignature.Effects;
    public static StatusData.EffectData[] GetDatas(this StatusEffect statusEffect)
    => statusEffect.StatusData.EffectsData;

    public static string Localized(this string text)
    => LocalizationManager.Instance.GetLoc(text);
    public static Item FirstItem(this GroupContainer stack)
    => stack.GetContainedItems()[0];
    public static KeyCode ToKeyCode(this string text)
    => GameInput.ToKeyCode(text);
    public static string ToName(this ControlsInput.GameplayActions action)
    => ControlsInput.GetGameplayActionName(action);
    public static string ToName(this ControlsInput.MenuActions action)
    => ControlsInput.GetMenuActionName(action);

    // Various
    public static Vector3 ToVector3(this (float X, float Y, float Z) t)
    => new(t.X, t.Y, t.Z);
    public static CodeMatcher CodeMatcher(this IEnumerable<CodeInstruction> t)
    => new(t);
    public static string SplitCamelCase(this string t)
    => Regex.Replace(t, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
    public static int SetBitCount(this int t)
    {
        int count = 0;
        while (t != 0)
        {
            count++;
            t &= t - 1;
        }
        return count;
    }
    public static bool TryFind(this Transform t, string name, out Transform result)
    {
        result = t.Find(name);
        return result != null;
    }
    public static bool Is<T>(this object t)
    => t is T;
    public static bool IsAny<T1, T2>(this object t)
    => t.Is<T1>() || t.Is<T2>();
    public static bool IsNot<T>(this object t)
    => !t.Is<T>();
    public static bool IsNotAny<T1, T2>(this object t)
    => t.IsNot<T1>() && t.IsNot<T2>();
    public static int ToInt(this string t)
    => Convert.ToInt32(t);
    public static T Random<T>(this IList<T> t)
    => t[UnityEngine.Random.Range(0, t.Count)];
    public static void Shuffle<T>(this IList<T> t)
    {
        for (int i = 0; i < t.Count - 1; ++i)
        {
            int j = UnityEngine.Random.Range(i, t.Count);
            (t[j], t[i]) = (t[i], t[j]);
        }
    }
    public static string FormatGameHours(this float t, bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true)
    {
        int days = t.Div(24).RoundDown();
        int hours = t.Mod(24).RoundDown();
        int minutes = t.Mod(1).Mul(60).RoundDown();
        int seconds = t.Mod(1f / 60).Mul(3600).RoundDown();
        return (showDays ? days.ToString("D2") + "d " : "") +
               (showHours ? hours.ToString("D2") + "h " : "") +
               (showMinutes ? minutes.ToString("D2") + "m " : "") +
               (showSeconds ? seconds.ToString("D2") + "s" : "");
    }
    public static string FormatGameHours(this double t, bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true)
    => ((float)t).FormatGameHours(showDays, showHours, showMinutes, showSeconds);
    public static string FormatSeconds(this float t, bool showMinutes = true, bool showSeconds = true)
    {
        int minutes = t.Div(60).RoundDown();
        int seconds = t.Mod(60).Round();
        return (showMinutes ? minutes.ToString() + "m " : "") +
               (showSeconds ? seconds.ToString() + "s" : "");
    }
    public static bool HasFlagsAttribute(this Enum enumeration)
    => enumeration.GetType().IsDefined(typeof(FlagsAttribute), false);
    public static void Append(this StringBuilder builder, params string[] texts)
    {
        foreach (var text in texts)
            builder.Append(text);
    }
    public static string SubstringBefore(this string text, string find, bool caseSensitive = true)
    {
        if (text.IsNotEmpty())
        {
            int length = text.IndexOf(find, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            if (length >= 1)
                return text.Substring(0, length);
        }
        return string.Empty;
    }
    public static bool ContainsSubstring(this string text, string find)
    => text.IsNotEmpty() && text.IndexOf(find) >= 0;
    public static T GetFirstComponentsInHierarchy<T>(this Transform root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null)
            return component;

        foreach (Transform child in root)
        {
            component = child.GetFirstComponentsInHierarchy<T>();
            if (component != null)
                return component;
        }

        return null;
    }
    public static List<T> GetAllComponentsInHierarchy<T>(this Transform root) where T : Component
    {
        List<T> components = new() { root.GetComponents<T>() };
        Utils.AppendChildrenRecurisvely(root, components);
        return components;
    }
    public static List<Component> GetAllComponentsInHierarchy<T1, T2>(this Transform root) where T1 : Component where T2 : Component
    {
        List<Component> components = new() { root.GetComponents<T1>(), root.GetComponents<T2>() };
        Utils.AppendChildrenRecurisvely(root, components);
        return components;
    }
    public static List<RaycastResult> GetMouseHits(this GraphicRaycaster t)
    {
        PointerEventData eventData = new(null) { position = Input.mousePosition };
        List<RaycastResult> hits = new();
        t.Raycast(eventData, hits);
        return hits;
    }
    public static int ToItemID(this string name)
    => Prefabs.ItemIDsByName[name];
    public static Item ToItem(this string name)
    => Prefabs.ItemsByID[Prefabs.ItemIDsByName[name].ToString()];
    public static int ToSkillID(this string name)
    => Prefabs.SkillIDsByName[name];
    public static Skill ToSkill(this string name)
    => Prefabs.SkillsByID[Prefabs.ItemIDsByName[name]];
    public static Tag ToTag(this string name)
    {
        foreach (var tag in TagSourceManager.Instance.m_tags)
            if (tag.TagName == name)
                return tag;
        return default;
    }
    public static bool IsDescendantOf(this GameObject t, GameObject a)
    {
        for (Transform i = t.transform.parent; i != null; i = i.parent)
            if (i == a.transform)
                return true;
        return false;
    }
    public static Transform FindAncestor(this GameObject t, Transform[] a)
    {
        for (Transform i = t.transform.parent; i != null; i = i.parent)
            if (i.IsContainedIn(a))
                return i;
        return null;
    }
    public static Transform FindAncestorWithComponent(this GameObject t, Type a)
    {
        for (Transform i = t.transform.parent; i != null; i = i.parent)
            if (i.GetComponent(a) != null)
                return i;
        return null;
    }
    public static Transform FindAncestorWithComponent(this GameObject t, Type[] a)
    {
        for (Transform i = t.transform.parent; i != null; i = i.parent)
            foreach (var type in a)
                if (i.GetComponent(type) != null)
                    return i;
        return null;
    }
    public static void TryAddLanternSlot(this Bag bag)
    {
        #region quit
        if (bag.HasLanternSlot)
            return;
        #endregion

        Bag adventurerBackpack = Prefabs.ItemsByID["5300000"] as Bag;
        GameObject lanternHolder = adventurerBackpack.LoadedVisual.FindChild("LanternSlotAnchor");
        GameObject newLanternHolder = GameObject.Instantiate(lanternHolder, bag.LoadedVisual.transform);
        bag.m_lanternSlot = newLanternHolder.GetComponentInChildren<BagSlotVisual>();
    }
    public static float RandomRange(this Vector2 t)
    => UnityEngine.Random.Range(t.x, t.y);

    // GOName
    public static string GOName(this Component t)
    => t.gameObject.name;
    public static string GOSetName(this Component t, string name)
    => t.gameObject.name = name;
    public static bool GONameIs(this Component t, string name)
    => t.gameObject.name == name;
    public static bool GOActive(this Component t)
    => t.gameObject.activeSelf;
    public static void GOSetActive(this Component t, bool state)
    => t.gameObject.SetActive(state);
    public static void GOToggle(this Component t)
    => t.GOSetActive(!t.GOActive());

    public static bool IsEmpty(this string text)
    => string.IsNullOrEmpty(text);
    public static bool IsNotEmpty(this string text)
    => !text.IsEmpty();
    public static float ToFloat(this string text)
        => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : float.NaN;

    public static void SetX(ref this Vector2 t, float a)
    => t.x = a;
    public static void SetY(ref this Vector2 t, float a)
    => t.y = a;
    public static void SetX(ref this Vector3 t, float a)
    => t.x = a;
    public static void SetY(ref this Vector3 t, float a)
    => t.y = a;
    public static void SetZ(ref this Vector3 t, float a)
    => t.z = a;
    public static void SetSizeZ(this BoxCollider t, float a)
    {
        Vector3 size = t.size;
        size.z = a;
        t.size = size;
    }

    public static Coroutine ExecuteAtTheEndOfFrame(this MonoBehaviour monoBehaviour, Action action)
    => monoBehaviour.StartCoroutine(Utils.CoroutineWaitUntilEndOfFrame(action));
    public static Coroutine ExecuteOnceAfterDelay(this MonoBehaviour monoBehaviour, float delay, Action action)
    => monoBehaviour.StartCoroutine(Utils.CoroutineWaitForSeconds(delay, action));
    public static Coroutine ExecuteOnceWhen(this MonoBehaviour monoBehaviour, Func<bool> test, Action action)
    => monoBehaviour.StartCoroutine(Utils.CoroutineWaitUntil(test, action));
    public static Coroutine ExecuteWhile(this MonoBehaviour monoBehaviour, Func<bool> test, Action action, Action finalAction = null)
    => monoBehaviour.StartCoroutine(Utils.CoroutineWhile(test, action, finalAction));
    public static Coroutine ExecuteUntil(this MonoBehaviour monoBehaviour, Func<bool> test, Action action, Action finalAction = null)
    => monoBehaviour.StartCoroutine(Utils.CoroutineDoUntil(test, action, finalAction));
}
