using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace DynamicEconomy
{
    class DESettings : ModSettings
    {
        public const float DefaultBuyingPriceFactorDropRate = 0.001f;           //Per tickLong
        public const float DefaultSellinPriceFactorGrowthRate = 0.0006f;
        public const float DefaultBuyingPriceFactorTechLevel = 0.25f;
        
        public static float buyingPriceFactorDropRate=0.001f;
        public static float buyingPriceFactorTechLevel=0.25f;
        public static float sellingPriceFactorGrowthRate=0.0006f;

        public static float costToDoublePriceMultipiler=1f;
        public static float costToHalvePriceMultipiler=1f;

        public static float turnoverEffectOnTraderCurrencyMultipiler=1f;
        public static float turnoverEffectDropRateMultipiler=1f;

        public static float randyCoinRandomOfsettMultipiler=1f;

        public static float orbitalTraderRandomPriceOffset=0.1f;

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("DE_Settings_PriceFactorDropRateMultiplier".Translate((buyingPriceFactorDropRate / DefaultBuyingPriceFactorDropRate).ToString("F2")));
            listingStandard.Label("DE_Settings_PriceMultiplier_HalvingTime".Translate(((int)Math.Log(0.5f, (1-buyingPriceFactorDropRate))*2000).ToStringTicksToDays()));
            buyingPriceFactorDropRate = listingStandard.Slider(buyingPriceFactorDropRate/DefaultBuyingPriceFactorDropRate, 0.01f, 10f)*DefaultBuyingPriceFactorDropRate;            //TODO make it logarithmic

            listingStandard.Label("DE_Settings_PriceFactorGrowthRateMultiplier".Translate((sellingPriceFactorGrowthRate / DefaultSellinPriceFactorGrowthRate).ToString("F2")));
            listingStandard.Label("DE_Settings_PriceMultiplier_DayToGrow".Translate(((int)(0.5f / sellingPriceFactorGrowthRate)*2000).ToStringTicksToDays()));
            sellingPriceFactorGrowthRate = listingStandard.Slider(sellingPriceFactorGrowthRate / DefaultSellinPriceFactorGrowthRate, 0.01f, 10f) * DefaultSellinPriceFactorGrowthRate;

            listingStandard.Label("DE_Settings_PriceFactor_TechLevel".Translate((buyingPriceFactorTechLevel / DefaultBuyingPriceFactorTechLevel).ToString("F2")));
            buyingPriceFactorTechLevel = listingStandard.Slider(buyingPriceFactorTechLevel / DefaultBuyingPriceFactorTechLevel, 0.01f, 10f) * DefaultBuyingPriceFactorTechLevel; 

            listingStandard.Label("DE_Settings_PurchaseEffectOnPriceMultipliers".Translate(costToDoublePriceMultipiler.ToString("F2")));
            listingStandard.Label("DE_Settings_PurchaseEffect_Silver".Translate((int)TradeablePriceModifier.CostToDoubleFactor));
            costToDoublePriceMultipiler = listingStandard.Slider(costToDoublePriceMultipiler, 0.05f, 10f);

            listingStandard.Label("DE_Settings_SaleEffectOnPriceMultipliers".Translate(costToHalvePriceMultipiler.ToString("F2")));
            listingStandard.Label("DE_Settings_SaleEffect_Silver".Translate((int)TradeablePriceModifier.CostToHalveFactor));
            costToHalvePriceMultipiler = listingStandard.Slider(costToHalvePriceMultipiler, 0.05f, 10f);

            listingStandard.Label("DE_Settings_TurnoverEffectOnTradersCurrency".Translate(turnoverEffectOnTraderCurrencyMultipiler.ToString("F2")));
            listingStandard.Label("DE_Settings_DealAmountToDoubleTradersCurrency".Translate((int)(turnoverEffectOnTraderCurrencyMultipiler * GameComponent_EconomyStateTracker.BaseTurnoverToDoubleTradersCurrency)));
            listingStandard.Label("DE_Settings_TurnoverEffect_Note".Translate());
            turnoverEffectOnTraderCurrencyMultipiler = listingStandard.Slider(turnoverEffectOnTraderCurrencyMultipiler, 0.05f, 10f);

            listingStandard.Label("DE_Settings_TurnoverEffectDropRateMultipliers".Translate(turnoverEffectDropRateMultipiler.ToString("F2")));
            listingStandard.Label("DE_Settings_CurrencyMultiplier_HalvingTime".Translate(((int)Math.Log(0.5f, (1 - turnoverEffectDropRateMultipiler*GameComponent_EconomyStateTracker.BaseTurnoverEffectDrop)) * 2000).ToStringTicksToDays()));
            turnoverEffectDropRateMultipiler = listingStandard.Slider(turnoverEffectDropRateMultipiler, 0.05f, 10f);

            listingStandard.Label("DE_Settings_PsicoinPriceRandomMultiplier".Translate(randyCoinRandomOfsettMultipiler.ToString("F2")));
            randyCoinRandomOfsettMultipiler = listingStandard.Slider(randyCoinRandomOfsettMultipiler, 0.01f, 10f);

            listingStandard.Label("DE_Settings_OrbitalTradersPriceOffsetRange".Translate(orbitalTraderRandomPriceOffset.ToString("F2")));
            orbitalTraderRandomPriceOffset = listingStandard.Slider(orbitalTraderRandomPriceOffset, 0f, 0.9f);

            listingStandard.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref buyingPriceFactorDropRate, "priceFDropRate", DefaultBuyingPriceFactorDropRate);
            Scribe_Values.Look(ref buyingPriceFactorTechLevel, "priceFTechLevel", DefaultBuyingPriceFactorTechLevel);
            Scribe_Values.Look(ref sellingPriceFactorGrowthRate, "priceFGrowthRate", DefaultSellinPriceFactorGrowthRate);
            Scribe_Values.Look(ref costToDoublePriceMultipiler, "costToDoublePriceMultipiler", 1f);
            Scribe_Values.Look(ref costToHalvePriceMultipiler, "costToHalvePriceMultipiler", 1f);
            Scribe_Values.Look(ref turnoverEffectOnTraderCurrencyMultipiler, "turnoverEffectOnTraderCurrencyMultipiler", 1f);
            Scribe_Values.Look(ref turnoverEffectDropRateMultipiler, "turnoverEffectDropRateMultipiler", 1f);
            Scribe_Values.Look(ref randyCoinRandomOfsettMultipiler, "randyCoinRandomOfsettMultipiler", 1f);
            Scribe_Values.Look(ref orbitalTraderRandomPriceOffset, "orbitalTraderRandomPriceOffset", 0.3f);
        }
    }
}
