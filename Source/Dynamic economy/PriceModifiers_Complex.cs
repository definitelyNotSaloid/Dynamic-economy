using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Xml;

namespace DynamicEconomy
{
    /// <summary>
    /// Contains all the info and provides methods needed to get price multipiler for any thing
    /// </summary>

    public class ComplexPriceModifier : IExposable
    {
        public List<ThingPriceModifier> thingPriceModifiers;
        public List<ThingCategoryPriceModifier> thingCategoryPriceModifiers;

        public static ModifierCategory GetModifierCategoryFor(ThingCategoryDef thingCategoryDef)
        {
            if (thingCategoryDef == null)
                return ModifierCategory.None;


            var extension = thingCategoryDef.GetModExtension<PriceModifierCategoryDefExtension>();
            if (extension != null && extension.category != ModifierCategory.None)
                return extension.category;

            return ModifierCategory.None;
        }

        public static ModifierCategory GetModifierCategoryFor(ThingDef thingDef, out ThingCategoryDef definingCategory)
        {
            if (thingDef == null)
            {
                definingCategory = null;
                return ModifierCategory.None;
            }


            var thingSpecificMod = thingDef.GetModExtension<PriceModifierCategoryDefExtension>();
            if (thingSpecificMod != null)
            {
                definingCategory = null;
                return thingSpecificMod.category;
            }

            if (thingDef.thingCategories==null || thingDef.thingCategories.Count == 0)
            {
                definingCategory = null;
                return ModifierCategory.None;
            }

            for (var i = 0; i < thingDef.thingCategories.Count; i++) {
                var node = thingDef.thingCategories[i];

                while (node != null) {
                    var extension = node.GetModExtension<PriceModifierCategoryDefExtension>();
                    if (extension != null && extension.category != ModifierCategory.None) {
                        definingCategory = node;
                        return extension.category;
                    }
                    node = node.parent;
                }
            }

            definingCategory = null;
            return ModifierCategory.None;
        }


        public virtual TradeablePriceModifier GetOrCreateIfNeededTradeablePriceModifier(ThingDef thingDef)         //returns null for ModifierCategory.None
        {
            if (thingDef == null) return null;

            ThingCategoryDef thingCategory;
            var modCategory = GetModifierCategoryFor(thingDef, out thingCategory);
            TradeablePriceModifier modifier = null;
            if (modCategory == ModifierCategory.Standalone)
            {
                modifier = thingPriceModifiers.Find(mod => mod.Def == thingDef);
                if (modifier == null)
                {
                    modifier = new ThingPriceModifier(thingDef);
                    thingPriceModifiers.Add((ThingPriceModifier)modifier);
                }
                else
                {
                    //Log.Message("Found modifier for " + thingDef.defName);
                }
            }
            else if (modCategory == ModifierCategory.Group)
            {
                modifier = thingCategoryPriceModifiers.Find(mod => mod.Def == thingCategory);
                if (modifier == null)
                {
                    modifier = new ThingCategoryPriceModifier(thingCategory);
                    thingCategoryPriceModifiers.Add((ThingCategoryPriceModifier)modifier);
                }
                else
                {
                    //Log.Message("Found modifier for " + thingDef.defName);
                }
            }


            return modifier;
        }

        public virtual ThingCategoryPriceModifier GetOrCreateIfNeededTradeablePriceModifier(ThingCategoryDef thingCategoryDef)         //returns null for ModifierCategory != Group
        {
            if (thingCategoryDef == null) return null;

            //var modCategory = GetModifierCategoryFor(thingCategoryDef);
            //ThingCategoryPriceModifier modifier;

            //if (modCategory == ModifierCategory.Group)
            //{
            var modifier = thingCategoryPriceModifiers.Find(mod => mod.Def == thingCategoryDef);
            if (modifier == null)
            {
                modifier = new ThingCategoryPriceModifier(thingCategoryDef);
                thingCategoryPriceModifiers.Add(modifier);
            }

            return modifier;
            //}
            //else
            //{
            //    Log.Error("Cant get modifier for " + modCategory.ToString() + "-type thing category defName=" + thingCategoryDef.defName);
            //    return null;
            //}
        }

        public ComplexPriceModifier()
        {
            //this.faction = faction;
            thingPriceModifiers = new List<ThingPriceModifier>();
            thingCategoryPriceModifiers = new List<ThingCategoryPriceModifier>();

        }

        public virtual float GetPriceMultipilerFor(ThingDef thingDef, TradeAction action, ConsideredFactors factor = ConsideredFactors.All)
        {
            // Changed so both modifiers applies

            var result = 1f;
            result *= thingPriceModifiers.Find(priceMod => priceMod.Def == thingDef)?.GetPriceMultipiler(action, factor) ?? 1f;

            if (thingDef.thingCategories == null) return result;
            foreach (var thingCat in thingDef.thingCategories) {
                var current = thingCat;
                while (current != null) {
                    var modifier = thingCategoryPriceModifiers.Find(priceMod => priceMod.Def == current);
                    if (modifier != null) return result * modifier.GetPriceMultipiler(action, factor);
                    current = current.parent;
                }
            }

            return result;
        }

        public virtual void RecordNewDeal(ThingDef thingDef, float totalCost, TradeAction action)
        {
            if (action == TradeAction.None)
                return;

            // The original code here won't correctly record for category modifiers
            var modifier = GetOrCreateIfNeededTradeablePriceModifier(thingDef);
            if (modifier != null)
                modifier.RecordNewDeal(action, totalCost);          // mb divide totalCost by current multipiler?
        }

        public virtual void TickLong()
        {
            thingPriceModifiers.RemoveAll(mod => mod == null);          //i have no idea where those null mods are coming from. TODO, but for now let it be. Its O(n) after all 

            for (int i = 0; i < thingPriceModifiers.Count; i++)
            { 
                thingPriceModifiers[i].TickLongUpdate();
                
            }

            thingCategoryPriceModifiers.RemoveAll(mod => mod == null);
            
            for (int i = 0; i < thingCategoryPriceModifiers.Count; i++)
            {
                thingCategoryPriceModifiers[i].TickLongUpdate();
            }
        }

        public void SetBaseModifier(ThingDef def, float baseSellFactor, float baseBuyFactor)
        {
            var modifier = GetOrCreateIfNeededTradeablePriceModifier(def);
            if (modifier == null)
            {
                Log.Error("tried to set base multipilers for None- or constant-category thing");    //TODO make const category available for setting base mods
                return;
            }

            modifier.SetBaseFactors(baseSellFactor, baseBuyFactor);
        }

        public void RegisterThingModifiers(List<BaseThingPriceMultipilerInfo> thingPriceMultipilers) {
            if (thingPriceMultipilers == null) return;
            foreach (var multiplier in thingPriceMultipilers) {
                var modifier = GetOrCreateIfNeededTradeablePriceModifier(DefDatabase<ThingDef>.GetNamed(multiplier.thingDefName));
                if (modifier != null) {
                    modifier.baseBuyFactor *= multiplier.buyMultiplier;
                    modifier.baseSellFactor *= multiplier.sellMultiplier;
                }
            }
        }

        public void RegisterCategoryModifiers(List<BaseCategoryPriceMultipilerInfo> categoryPriceMultipilers) {
            if (categoryPriceMultipilers == null) return;
            foreach (var multiplier in categoryPriceMultipilers) {
                var modifier = GetOrCreateIfNeededTradeablePriceModifier(DefDatabase<ThingCategoryDef>.GetNamed(multiplier.categoryDefName));
                if (modifier != null) {
                    modifier.baseBuyFactor *= multiplier.buyMultiplier;
                    modifier.baseSellFactor *= multiplier.sellMultiplier;
                }
            }
        }

        public void AddEventModifier(ThingCategoryDef def, float playerSellsFactor, float playerBuysFactor)
        {
            var modifier = GetOrCreateIfNeededTradeablePriceModifier(def);
            modifier.SetEventFactors(playerSellsFactor, playerBuysFactor);
        }

        public virtual void ExposeData()
        {
            Scribe_Collections.Look(ref thingCategoryPriceModifiers, "thingCategoryMods", LookMode.Deep);
            Scribe_Collections.Look(ref thingPriceModifiers, "thingsMods", LookMode.Deep);
        }
    }

    public class TraderCaravansPriceModifier : ComplexPriceModifier
    {
        public Faction faction;
        public TraderCaravansPriceModifier() : base() { }
        public TraderCaravansPriceModifier(Faction faction) : base() { 
            this.faction = faction;
            if (faction == null) return;

            var extension = faction.def.GetModExtension<LocalPriceModifierDefExtension>();
            if (extension != null) {
                RegisterCategoryModifiers(extension.categoryPriceMultipilers);
                RegisterThingModifiers(extension.thingPriceMultipilers);
                return;
            } else Log.Warning("Havent found any faction modifier for " + faction.def.defName);
        }

        public override float GetPriceMultipilerFor(ThingDef thingDef, TradeAction action, ConsideredFactors factor = ConsideredFactors.All) {
            var result = base.GetPriceMultipilerFor(thingDef, action, factor);

            if (faction == null) return result;

            if (action == TradeAction.PlayerBuys && (factor == ConsideredFactors.All || factor == ConsideredFactors.Base)) {
                if (thingDef.techLevel > faction.def.techLevel + 1)
                    result = result * (1 + DESettings.buyingPriceFactorTechLevel * (thingDef.techLevel - faction.def.techLevel - 1));
            }

            return result;
        }

        public override void ExposeData() 
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
        }
    }



    public class SettlementPriceModifier : ComplexPriceModifier
    {
        public Settlement settlement;
        public SettlementPriceModifier() : base() { }   //only for easier exposing
        public SettlementPriceModifier(Settlement settlement) : base()                  
        {
            if (settlement != null && settlement.Faction != Faction.OfPlayer)
            {
                this.settlement = settlement;




                var hills = Find.WorldGrid[settlement.Tile].hilliness;
                ConstantPriceModsDef hillModDef;
                switch (hills)
                {

                    case Hilliness.Flat:
                        hillModDef = DynamicEconomyDefOf.Hillness_Flat;
                        break;

                    case Hilliness.SmallHills:
                        hillModDef = DynamicEconomyDefOf.Hillness_SmallHills;
                        break;

                    case Hilliness.LargeHills:
                        hillModDef = DynamicEconomyDefOf.Hillness_LargeHills;
                        break;

                    case Hilliness.Mountainous:
                        hillModDef = DynamicEconomyDefOf.Hillness_Mountainous;
                        break;

                    default:
                        Log.Error("Settlement is placed in " + hills.ToString() + " region for which there is no base modifier");
                        hillModDef = null;
                        break;

                }

                if (hillModDef != null)
                {
                    // it is safe to add mods directly since lists are empty at this moment
                    RegisterCategoryModifiers(hillModDef.categoryPriceMultipilers);
                    RegisterThingModifiers(hillModDef.thingPriceMultipilers);
                    //foreach (var mod in hillModDef.categoryPriceMultipilers)
                    //{
                    //    thingCategoryPriceModifiers.Add(new ThingCategoryPriceModifier(
                    //        DefDatabase<ThingCategoryDef>.GetNamed(mod.categoryDefName),
                    //        mod.baseMultipiler,
                    //        mod.baseMultipiler));
                    //}

                    //foreach (var mod in hillModDef.thingPriceMultipilers)
                    //{
                    //    thingPriceModifiers.Add(new ThingPriceModifier(
                    //        DefDatabase<ThingDef>.GetNamed(mod.thingDefName),
                    //        mod.baseMultipiler,
                    //        mod.baseMultipiler));
                    //}
                }


                var extension = settlement.Biome.GetModExtension<LocalPriceModifierDefExtension>();
                if (extension != null) {
                    RegisterCategoryModifiers(extension.categoryPriceMultipilers);
                    RegisterThingModifiers(extension.thingPriceMultipilers);
                    return;
                } else Log.Warning("Havent found any biome modifier for " + settlement.Biome.defName);

                var faction = settlement.Faction.def.GetModExtension<LocalPriceModifierDefExtension>();
                if (faction != null) {
                    Log.Warning("Found faction modifier for " + settlement.Faction.def.defName + " / " + faction.categoryPriceMultipilers.Count);
                    RegisterCategoryModifiers(faction.categoryPriceMultipilers);
                    RegisterThingModifiers(faction.thingPriceMultipilers);
                    return;
                } else Log.Warning("Havent found any faction modifier for " + settlement.Faction.def.defName);

                //foreach (var biomeMod in extension.thingPriceMultipilers)
                //{
                //    var mod = GetOrCreateIfNeededTradeablePriceModifier(DefDatabase<ThingDef>.GetNamed(biomeMod.thingDefName));
                //    if (mod != null)
                //    {
                //        mod.baseBuyFactor *= biomeMod.baseMultipiler;
                //        mod.baseSellFactor *= biomeMod.baseMultipiler;
                //    }
                //}

                //foreach (var biomeMod in extension.categoryPriceMultipilers)
                //{
                //    var mod = GetOrCreateIfNeededTradeablePriceModifier(DefDatabase<ThingCategoryDef>.GetNamed(biomeMod.categoryDefName));
                //    if (mod != null)
                //    {
                //        mod.baseBuyFactor *= biomeMod.baseMultipiler;
                //        mod.baseSellFactor *= biomeMod.baseMultipiler;
                //    }
                //}


            }
            else
            {
                throw new ArgumentException("Cant set settlement modifier for null- or player settlement");
            }
        }

        public override float GetPriceMultipilerFor(ThingDef thingDef, TradeAction action, ConsideredFactors factor = ConsideredFactors.All) {
            var result = base.GetPriceMultipilerFor(thingDef, action, factor);

            if (settlement?.Faction == null) return result;

            if (action == TradeAction.PlayerBuys && (factor == ConsideredFactors.All || factor == ConsideredFactors.Base)) {
                var Faction = settlement.Faction.def;
                if (thingDef.techLevel > Faction.techLevel + 1) {
                    result = result * (1 + DESettings.buyingPriceFactorTechLevel * (thingDef.techLevel - Faction.techLevel - 1));
                }
            }

            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }
    }

    public class OrbitalTraderPriceModifier : ComplexPriceModifier
    {
        public TradeShip ship;

        public OrbitalTraderPriceModifier() : base() { }        //only for easier exposing
        public OrbitalTraderPriceModifier(TradeShip tradeShip) : base()
        {
            ship = tradeShip;
        }

        public override ThingCategoryPriceModifier GetOrCreateIfNeededTradeablePriceModifier(ThingCategoryDef thingCategoryDef)
        {
            var res = base.GetOrCreateIfNeededTradeablePriceModifier(thingCategoryDef);
            if (res == null)
                return null;

            if (DESettings.orbitalTraderRandomPriceOffset!=0f && res.GetPriceMultipiler(TradeAction.PlayerBuys, ConsideredFactors.Base)==1f)     //if newly created
            {
                float randBase = 1 + Rand.Sign * Rand.Value * DESettings.orbitalTraderRandomPriceOffset;    
                res.SetBaseFactors(randBase, randBase);
            }

            return res;
        }
        public override TradeablePriceModifier GetOrCreateIfNeededTradeablePriceModifier(ThingDef thingDef)
        {
            var res = base.GetOrCreateIfNeededTradeablePriceModifier(thingDef);
            if (res == null)
                return null;

            if (res.GetPriceMultipiler(TradeAction.PlayerBuys, ConsideredFactors.Base) == 1f)     //if newly created
            {
                float randBase = 1 + Rand.Sign * Rand.Value * DESettings.orbitalTraderRandomPriceOffset;     
                res.SetBaseFactors(randBase, randBase);
            }

            return res;
        }

        public override float GetPriceMultipilerFor(ThingDef thingDef, TradeAction action, ConsideredFactors factor = ConsideredFactors.All)
        {
            var mod = GetOrCreateIfNeededTradeablePriceModifier(thingDef);
            if (mod == null)
                return 1f;

            return mod.GetPriceMultipiler(action, factor);
        }

        public override void RecordNewDeal(ThingDef thingDef, float totalCost, TradeAction action)
        {
            // nothing. literally. trader will leave soon and player will never see them again, no reason to waste resources on dynamic factor adjustments
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ship, "ship");
        }
    }
}
