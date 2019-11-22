﻿using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace RWBYRemnant
{
    public class TakePhotoComp : CompProperties
    {
        public Color LightCopyColor;
        public ThingDef usesAmmunition;

        public TakePhotoComp()
        {
            compClass = typeof(Weapon_TakePhotoAbility);
        }
    }

    public class Weapon_TakePhotoAbility : CompUseEffect
    {
        public TakePhotoComp Props => (TakePhotoComp)props;

        public CompEquippable GetEquippable => parent.GetComp<CompEquippable>();

        public Pawn GetPawn => GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        public HashSet<string> ListOfDifferentPhotos = new HashSet<string>();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref ListOfDifferentPhotos, false,  parent.ThingID.ToString() + "ListOfDifferentPhotos");
            if (ListOfDifferentPhotos == null)
            {
                ListOfDifferentPhotos = new HashSet<string>();
            }
        }

        public void CreateLightCopy(string defname)
        {
            if (GetPawn.equipment.Primary != null) //if primary equipped
            {
                if (GetPawn.equipment.Primary.TryGetComp<Weapon_TakePhotoAbility>() == null) // if primary not camera
                {
                    if (GetPawn.equipment.Primary.TryGetComp<LightCopyDestroyAbility>() != null) // if primary light copy
                    {
                        GetPawn.equipment.Primary.Destroy(); // destroy light copy
                    }
                    else
                    { 
                        return; // primary is no camera or light copy
                    }
                }      
                else
                {
                    MakeBox(); // primary is camera
                }
            }

            // new weapon with specific color
            ThingWithComps weaponToCreate = new ThingWithComps();
            if (ThingDef.Named(defname).MadeFromStuff)
            {
                weaponToCreate = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named(defname), GenStuff.RandomStuffFor(ThingDef.Named(defname)));
            }
            else
            {
                weaponToCreate = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named(defname), null);
            }
            if (weaponToCreate.TryGetComp<CompQuality>() != null)
            {
                weaponToCreate.TryGetComp<CompQuality>().SetQuality(parent.TryGetComp<CompQuality>().Quality, ArtGenerationContext.Colony);
            }
            weaponToCreate.HitPoints = 1;            
            if (weaponToCreate.TryGetComp<CompColorable>() == null)
            {
                CompColorable compColorable = new CompColorable
                {
                    parent = weaponToCreate
                };
                CompProperties compProps = new CompProperties
                {
                    compClass = typeof(CompColorable)
                };
                compColorable.Initialize(compProps);
                weaponToCreate.AllComps.Add(compColorable);
            }
            weaponToCreate.TryGetComp<CompColorable>().Color = Props.LightCopyColor;

            // remove certain special abilities from light copy
            weaponToCreate.AllComps.RemoveAll(x => x.props.GetType().Equals(typeof(WeaponTransformComp)));
            weaponToCreate.AllComps.RemoveAll(x => x.props.GetType().Equals(typeof(TakePhotoComp)));

            LightCopyDestroyAbility rWBYLightCopyDestroyAbility = new LightCopyDestroyAbility
            {
                parent = weaponToCreate
            };
            CompProperties compProps2 = new CompProperties
            {
                compClass = typeof(LightCopyDestroyAbility)
            };
            rWBYLightCopyDestroyAbility.Initialize(compProps2);
            weaponToCreate.AllComps.Add(rWBYLightCopyDestroyAbility);

            if (!ConsumeAmmunition()) return;
            ListOfDifferentPhotos.Remove(defname);
            GetPawn.equipment.AddEquipment(weaponToCreate);
        }

        private void MakeBox()
        {
            ThingWithComps cameraBox = (ThingWithComps)ThingMaker.MakeThing(RWBYDefOf.RWBY_Velvet_Camera_Box, null);
            cameraBox.TryGetComp<CompQuality>().SetQuality(parent.TryGetComp<CompQuality>().Quality, ArtGenerationContext.Colony);
            cameraBox.TryGetComp<Weapon_TakePhotoAbility>().ListOfDifferentPhotos = ListOfDifferentPhotos;
            cameraBox.HitPoints = parent.HitPoints;
            parent.Destroy();
            cameraBox.ThingID = parent.ThingID;
            cameraBox.thingIDNumber = parent.thingIDNumber;
            GetPawn.equipment.AddEquipment(cameraBox);
        }

        public void ClearPhotos()
        {
            ListOfDifferentPhotos.Clear();
        }

        public bool ConsumeAmmunition()
        {
            Thing thing = GetPawn.inventory.GetDirectlyHeldThings().ToList().Find(s => s.def == Props.usesAmmunition);
            if (Props.usesAmmunition == null)
            {
                return true;
            }
            else if (Props.usesAmmunition != null && thing == null)
            {
                return false;
            }
            else
            {
                thing.stackCount = thing.stackCount - 1;
                if (thing.stackCount == 0) thing.Destroy();
                return true;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                action = ClearPhotos,
                defaultLabel = "LightCopyPhotoClearLabel".Translate(),
                defaultDesc = "LightCopyPhotoClearDescription".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                disabled = ListOfDifferentPhotos.Count == 0,
                disabledReason = "LightCopyPhotoClearDisabled".Translate(),
                activateSound = SoundDefOf.CancelMode
            };

            foreach (string photoName in ListOfDifferentPhotos)
            {
                bool disabled = false;
                string disabledReason = "";

                if (GetPawn == null)
                {
                    disabled = true;
                }
                else if (!GetPawn.equipment.AllEquipmentListForReading.Contains(parent))
                {
                    disabled = true;
                    disabledReason = "DisabledNotEquipped".Translate(parent.def.label);
                }
                else if (Props.usesAmmunition != null && GetPawn.inventory.GetDirectlyHeldThings().ToList().Find(s => s.def == Props.usesAmmunition) == null)
                {
                    disabled = true;
                    disabledReason = "DisabledNoDustPowderAmmunition".Translate(Props.usesAmmunition.label).CapitalizeFirst();
                }

                yield return new Command_Action
                {
                    action = delegate ()
                    {
                        CreateLightCopy(photoName);
                    },
                    defaultLabel = "LightCopyLabel".Translate(ThingDef.Named(photoName).label),
                    defaultDesc = "LightCopyDescription".Translate(ThingDef.Named(photoName).label),
                    icon = ThingDef.Named(photoName).uiIcon,
                    defaultIconColor = Props.LightCopyColor,
                    disabled = disabled,
                    disabledReason = disabledReason,
                    alsoClickIfOtherInGroupClicked = false
                };
            }
        }
    }
}
