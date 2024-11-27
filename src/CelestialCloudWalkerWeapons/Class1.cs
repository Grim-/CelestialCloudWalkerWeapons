using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CelestialCloudWalkerWeapons
{
    public class CompProperties_RangeToggle : CompProperties
    {
        public CompProperties_RangeToggle()
        {
            this.compClass = typeof(CompRangeToggle);
            Log.Message("[RangeToggle] CompProperties_RangeToggle constructor called");
        }
    }

    public class CompRangeToggle : ThingComp
    {
        private bool extendedRangeEnabled = false;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            var equipmentComp = parent.GetComp<CompEquippable>();
            var extendedVerb = equipmentComp.AllVerbs
                .FirstOrDefault(v => v is Verb_ExtendedMeleeAttack) as Verb_ExtendedMeleeAttack;

            if (extendedVerb != null)
            {
                extendedVerb.SetRange(500f);
                Log.Message($"[RangeToggle] Range set to: {extendedVerb.EffectiveRange}");
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref extendedRangeEnabled, "extendedRangeEnabled", false);
        }


        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Log.Message($"CompGetGizmosExtra");

            foreach (var item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }

            yield return ToggleAction();
        }


        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Log.Message($"CompGetGizmosExtra");

            foreach (var item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            yield return ToggleAction();
        }

        private Command_Action ToggleAction()
        {
            return new Command_Action
            {
                defaultLabel = "Extended Range",
                defaultDesc = "Toggle extended range attacks",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                action = () =>
                {
                    extendedRangeEnabled = !extendedRangeEnabled;
                    var equipmentComp = parent.GetComp<CompEquippable>();
                    var extendedVerb = equipmentComp.AllVerbs
                        .FirstOrDefault(v => v is Verb_ExtendedMeleeAttack) as Verb_ExtendedMeleeAttack;

                    Log.Message($"[RangeToggle] Toggle pressed. EquipmentComp: {equipmentComp != null}, ExtendedVerb found: {extendedVerb != null}");

                    if (extendedVerb != null)
                    {
                        extendedVerb.SetRange(extendedRangeEnabled ? 500f : 1.5f);
                        Log.Message($"[RangeToggle] Range set to: {extendedVerb.EffectiveRange}");
                    }
                }
            };
        }
    }

    public class JobDriver_ExtendedMeleeAttack : JobDriver_AttackMelee
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            base.AddFinishAction(delegate (JobCondition jobCondition)
            {
                if (!this.pawn.IsPlayerControlled || !this.pawn.Drafted)
                {
                    return;
                }
                if (!this.job.playerInterruptedForced)
                {
                    Thing targetThingA = base.TargetThingA;
                    if (targetThingA != null && targetThingA.def.autoTargetNearbyIdenticalThings)
                    {
                        foreach (IntVec3 c in GenRadial.RadialCellsAround(base.TargetThingA.Position, 4f, false).InRandomOrder(null))
                        {
                            if (c.InBounds(base.Map))
                            {
                                foreach (Thing thing in c.GetThingList(base.Map))
                                {
                                    if (thing.def == base.TargetThingA.def && this.pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn) && this.pawn.jobs.jobQueue.Count == 0)
                                    {
                                        Job job = this.job.Clone();
                                        job.targetA = thing;
                                        this.pawn.jobs.jobQueue.EnqueueFirst(job, null);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            });

            yield return Toils_General.DoAtomic(delegate
            {
                Pawn pawn;
                if ((pawn = (this.job.targetA.Thing as Pawn)) == null)
                {
                    return;
                }
                bool flag = pawn.Downed && this.pawn.mindState.duty != null && this.pawn.mindState.duty.attackDownedIfStarving && this.pawn.Starving();
                CompActivity compActivity;
                bool flag2 = ModsConfig.AnomalyActive && pawn.TryGetComp(out compActivity) && compActivity.IsDormant;
                if (flag || flag2)
                {
                    this.job.killIncappedTarget = true;
                }
            });

            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Verb verb = this.job.verbToUse ?? pawn.meleeVerbs.TryGetMeleeVerb(TargetThingA);
            float range = verb?.verbProps?.range ?? 1.42f;

            yield return Toils_Combat.GotoCastPosition(TargetIndex.A, TargetIndex.None, true, range).FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}
