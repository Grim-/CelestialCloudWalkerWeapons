using Verse;

namespace CelestialCloudWalkerWeapons
{
    public class DamageWorker_MultiDamage : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult primaryResult = base.Apply(dinfo, victim);

            MultiDamageDef multiDef = def as MultiDamageDef;
            if (multiDef?.secondaryDamages == null || victim == null)
                return primaryResult;

            foreach (DamageData damageData in multiDef.secondaryDamages)
            {
                DamageInfo secondaryDinfo = new DamageInfo(
                    def: damageData.damage,
                    amount: dinfo.Amount * damageData.multiplier,
                    armorPenetration: dinfo.ArmorPenetrationInt,
                    angle: dinfo.Angle,
                    instigator: dinfo.Instigator,
                    hitPart: dinfo.HitPart,
                    weapon: dinfo.Weapon,
                    category: dinfo.Category
                );

                secondaryDinfo.SetBodyRegion(dinfo.Height, dinfo.Depth);
                victim.TakeDamage(secondaryDinfo);
            }

            return primaryResult;
        }
    }
}
