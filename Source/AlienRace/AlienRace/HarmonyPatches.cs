﻿using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AlienRace
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.erdelf.alien_race.main");

            harmony.Patch(AccessTools.Method(typeof(PawnHairChooser), "RandomHairDefFor"), new HarmonyMethod(typeof(HarmonyPatches), "RandomHairDefForPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) }), new HarmonyMethod(typeof(HarmonyPatches), "GeneratePawnPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateBodyType"), null, new HarmonyMethod(typeof(HarmonyPatches), "GenerateBodyTypePostfix"));
            harmony.Patch(AccessTools.Property(typeof(Pawn_StoryTracker), "SkinColor").GetGetMethod(), null, new HarmonyMethod(typeof(HarmonyPatches), "SkinColorPostfix"));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateTraits"), new HarmonyMethod(typeof(HarmonyPatches), "GenerateTraitsPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), "ResolveAllGraphics"), new HarmonyMethod(typeof(HarmonyPatches), "ResolveAllGraphicsPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"), new HarmonyMethod(typeof(HarmonyPatches), "TryGiveSolidBioToPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "SetBackstoryInSlot"), new HarmonyMethod(typeof(HarmonyPatches), "SetBackstoryInSlotPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new Type[] {typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool)}), new HarmonyMethod(typeof(HarmonyPatches), "RenderPawnInternalPrefix"), null);
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("AgeInjuryUtility"), "GenerateRandomOldAgeInjuries"), new HarmonyMethod(typeof(HarmonyPatches), "GenerateRandomOldAgeInjuriesPrefix"), null);
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("AgeInjuryUtility"), "RandomHediffsToGainOnBirthday", new Type[] {typeof(Pawn), typeof(int)}), new HarmonyMethod(typeof(HarmonyPatches), "RandomHediffsToGainOnBirthdayPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(StartingPawnUtility), "NewGeneratedStartingPawn"), new HarmonyMethod(typeof(HarmonyPatches), "NewGeneratedStartingPawnPrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GiveAppropriateBioAndNameTo"), null, new HarmonyMethod(typeof(HarmonyPatches), "GiveAppropriateBioAndNameToPostfix"));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateRandomAge"), new HarmonyMethod(typeof(HarmonyPatches), "GenerateRandomAgePrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GeneratePawnName"), new HarmonyMethod(typeof(HarmonyPatches), "GeneratePawnNamePrefix"), null);
            harmony.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "CanDoNext"), null, new HarmonyMethod(typeof(HarmonyPatches), "CanDoNextStartPawnPostfix"));
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), "CanGetThought"), null, new HarmonyMethod(typeof(HarmonyPatches), "CanGetThoughtPostfix"));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("AddHumanlikeOrdersPostfix")));

            DefDatabase<HairDef>.GetNamed("Shaved").hairTags.Add("alienNoHair"); // needed because..... the original idea doesn't work and I spend enough time finding a good solution
        }

        public static void AddHumanlikeOrdersPostfix(ref List<FloatMenuOption> opts, Pawn pawn, Vector3 clickPos)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);

            if (pawn.equipment != null)
            {
                Apparel apparel = pawn.Map.thingGrid.ThingAt<Apparel>(c);
                if (apparel != null)
                {
                    List<FloatMenuOption> options = opts.Where(fmo => !fmo.Disabled && DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Any(d => pawn.def != d && !((pawn.def is ThingDef_AlienRace) && !(pawn.def as ThingDef_AlienRace).raceRestrictedApparel.NullOrEmpty() && (pawn.def as ThingDef_AlienRace).raceRestrictedApparel.Contains(apparel.def)) && !d.raceRestrictedApparel.NullOrEmpty() && d.raceRestrictedApparel.Contains(apparel.def))).ToList();

                    if (!options.NullOrEmpty())
                    {
                        foreach (FloatMenuOption fmo in options)
                        {
                            int index = opts.IndexOf(fmo);
                            opts.Remove(fmo);

                            opts.Insert(index, new FloatMenuOption("CannotWear".Translate(new object[]
                                {
                                    apparel.LabelShort
                                }) + " (" + pawn.def.LabelCap + " can't wear this" + ")", null, MenuOptionPriority.Default, null, null, 0f, null));
                        }
                    }


                    ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;

                    if (alienProps != null && alienProps.onlyUseRacerestrictedApparel)
                    {
                        options = opts.Where(fmo => !fmo.Disabled && (alienProps.raceRestrictedApparel.NullOrEmpty() || !alienProps.raceRestrictedApparel.Contains(apparel.def))).ToList();

                        if (!options.NullOrEmpty())
                        {
                            foreach (FloatMenuOption fmo in options)
                            {
                                int index = opts.IndexOf(fmo);
                                opts.Remove(fmo);

                                opts.Insert(index, new FloatMenuOption("CannotWear".Translate(new object[]
                                    {
                                        apparel.LabelShort
                                    }) + " (" + alienProps.LabelCap + " can't use other races' apparel" + ")", null, MenuOptionPriority.Default, null, null, 0f, null));
                            }
                        }
                    }
                }
            }
        }

        public static void CanGetThoughtPostfix(ThoughtHandler __instance, ref bool __result, ThoughtDef def)
        {
            ThingDef_AlienRace alienProps = __instance.pawn.def as ThingDef_AlienRace;
            if (__result && def == ThoughtDefOf.Naked && alienProps != null)
                if (!alienProps.cannotReceiveThoughts.NullOrEmpty() && alienProps.cannotReceiveThoughts.Contains(def))
                    __result = false;
        }

        public static void CanDoNextStartPawnPostfix(ref bool __result)
        {
            if (__result)
                return;
            bool result = true;
            Find.GameInitData.startingPawns.ForEach(current =>
            {
                if (!current.Name.IsValid && current.def.race.GetNameGenerator(current.gender) == null)
                    result = false;
            });
            __result = result;
        }

        public static bool GeneratePawnNamePrefix(ref Name __result, Pawn pawn, NameStyle style = NameStyle.Full, string forcedLastName = null)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if(alienProps == null || alienProps.race.GetNameGenerator(pawn.gender) == null || style != NameStyle.Full)
                return true;

            NameTriple nameTriple = NameTriple.FromString(NameGenerator.GenerateName(alienProps.race.GetNameGenerator(pawn.gender)));

            string first = nameTriple.First, nick = nameTriple.Nick, last = nameTriple.Last;

            if (nick == null)
                nick = nameTriple.First;

            if (last != null && forcedLastName != null)
                last = forcedLastName;

            __result = new NameTriple(first, nick, last);

            return false;
        }

        public static void GenerateRandomAgePrefix(Pawn pawn, PawnGenerationRequest request)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;

            if (alienProps != null && alienProps.MaleGenderProbability != 0.5f)
            {
                if (!request.FixedGender.HasValue)
                    pawn.gender = Rand.RangeInclusive(0, 100) >= alienProps.MaleGenderProbability ? Gender.Female : Gender.Male;
                else
                    pawn.GetComp<AlienPartGenerator.AlienComp>().fixGenderPostSpawn = true;
            }
        }

        public static void GiveAppropriateBioAndNameToPostfix(Pawn pawn)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;

            if (alienProps != null)
            {
                //Log.Message(pawn.LabelCap);
                if (alienProps.alienPartGenerator.alienhaircolorgen != null)
                    pawn.story.hairColor = alienProps.alienPartGenerator.alienhaircolorgen.NewRandomizedColor();

                if (alienProps.GetsGreyAt <= pawn.ageTracker.AgeBiologicalYears)
                {
                    float grey = Rand.Range(0.65f, 0.85f);
                    pawn.story.hairColor = new Color(grey, grey, grey);
                }
                if (!alienProps.NakedHeadGraphicLocation.NullOrEmpty())
                {
                    Traverse.Create(pawn.story).Field("headGraphicPath").SetValue(alienProps.alienPartGenerator.RandomAlienHead(alienProps.NakedHeadGraphicLocation, pawn.gender));
                }
            }
        }

        public static bool NewGeneratedStartingPawnPrefix(ref Pawn __result)
        {
            PawnKindDef kindDef = Faction.OfPlayer.def.basicMemberKind;

            DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Where(tdar => !tdar.startingColonists.NullOrEmpty()).
                SelectMany(tdar => tdar.startingColonists).Where(sce => sce.factionDefs.Contains(Faction.OfPlayer.def)).SelectMany(sce => sce.pawnKindEntries).InRandomOrder().ToList().ForEach(pke =>
                {
                    if (Rand.Range(0f, 100f) < pke.chance)
                    {
                        kindDef = pke.kindDefs.RandomElement();
                    }
                });

            if (kindDef == Faction.OfPlayer.def.basicMemberKind)
                return true;

            PawnGenerationRequest request = new PawnGenerationRequest(kindDef, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, null, true, false, false, false, true, false, 26f, false, true, true, null, null, null, null, null, null);
            Pawn pawn = null;
            try
            {
                pawn = PawnGenerator.GeneratePawn(request);
            }
            catch (Exception arg)
            {
                Log.Error("There was an exception thrown by the PawnGenerator during generating a starting pawn. Trying one more time...\nException: " + arg);
                pawn = PawnGenerator.GeneratePawn(request);
            }
            pawn.relations.everSeenByPlayer = true;
            PawnComponentsUtility.AddComponentsForSpawn(pawn);
            __result = pawn;

            return false;
        }

        public static bool RandomHediffsToGainOnBirthdayPrefix(ref IEnumerable<HediffGiver_Birthday> __result, Pawn pawn)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null && alienProps.ImmuneToAge)
            {
                __result = new List<HediffGiver_Birthday>();
                return false;
            }
            return true;
        }

        public static bool GenerateRandomOldAgeInjuries(Pawn pawn)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null && alienProps.ImmuneToAge)
                return false;
            return true;
        }
        
        public static bool SetBackstoryInSlotPrefix(Pawn pawn, BackstorySlot slot, ref Backstory backstory)
        {

            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;

            if (alienProps != null && alienProps.PawnsSpecificBackstories && !pawn.kindDef.backstoryCategory.NullOrEmpty())
                if (BackstoryDatabase.allBackstories.Where(kvp => kvp.Value.shuffleable && kvp.Value.spawnCategories.Contains(pawn.kindDef.backstoryCategory) &&
                 kvp.Value.slot == slot && (slot != BackstorySlot.Adulthood ||
                 !kvp.Value.requiredWorkTags.OverlapsWithOnAnyWorkType(pawn.story.childhood.workDisables))).Select(kvp => kvp.Value).TryRandomElement(out backstory))
                    return false;
            return true;
        }

        public static bool TryGiveSolidBioToPrefix(Pawn pawn, ref bool __result)
        {
            if(pawn.def is ThingDef_AlienRace)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static void GiveShortHashPrefix(ref Def def)
        {
            if (def is BackstoryDef)
                if (def.shortHash != 0)
                    def.shortHash = 0;
        }

        public static bool ResolveAllGraphicsPrefix(PawnGraphicSet __instance)
        {
            ThingDef_AlienRace alienProps = __instance.pawn.def as ThingDef_AlienRace;
            if (alienProps != null)
            {
                if (__instance.pawn.GetComp<AlienPartGenerator.AlienComp>().fixGenderPostSpawn)
                {
                    if (alienProps.MaleGenderProbability != 0.5f)
                            __instance.pawn.gender = Rand.RangeInclusive(0, 100) >= alienProps.MaleGenderProbability ? Gender.Female : Gender.Male;

                    if (!alienProps.NakedHeadGraphicLocation.NullOrEmpty())
                        Traverse.Create(__instance.pawn.story).Field("headGraphicPath").SetValue(alienProps.alienPartGenerator.RandomAlienHead(alienProps.NakedHeadGraphicLocation, __instance.pawn.gender));

                     __instance.pawn.GetComp<AlienPartGenerator.AlienComp>().fixGenderPostSpawn = false;
                }

                __instance.nakedGraphic = AlienPartGenerator.GetNakedGraphic(__instance.pawn.story.bodyType, ShaderDatabase.Cutout, __instance.pawn.story.SkinColor, alienProps.NakedBodyGraphicLocation);
                __instance.rottingGraphic = AlienPartGenerator.GetNakedGraphic(__instance.pawn.story.bodyType, ShaderDatabase.Cutout, PawnGraphicSet.RottingColor, alienProps.NakedBodyGraphicLocation);
                __instance.dessicatedGraphic = GraphicDatabase.Get<Graphic_Multi>(alienProps.DesiccatedGraphicLocation, ShaderDatabase.Cutout);
                __instance.headGraphic = !alienProps.NakedHeadGraphicLocation.NullOrEmpty() ? GraphicDatabase.Get<Graphic_Multi>(__instance.pawn.story.HeadGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, __instance.pawn.story.SkinColor) : null;
                __instance.desiccatedHeadGraphic = GraphicDatabase.Get<Graphic_Multi>(__instance.pawn.story.HeadGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, PawnGraphicSet.RottingColor);
                __instance.skullGraphic = GraphicDatabase.Get<Graphic_Multi>(alienProps.SkullGraphicLocation, ShaderDatabase.CutoutSkin, Vector2.one, Color.white);
                __instance.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(__instance.pawn.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, __instance.pawn.story.hairColor);

                __instance.ResolveApparelGraphics();
                return false;
            }
            else
                return true;
        }

        public static void GenerateTraitsPrefix(Pawn pawn)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null && !alienProps.forcedRaceTraitEntries.NullOrEmpty())
                foreach (AlienTraitEntry ate in alienProps.forcedRaceTraitEntries)
                    if (Rand.Range(0, 100) < ate.chance)
                        if((pawn.gender == Gender.Male && (ate.commonalityMale == -1f || Rand.Range(0,100) < ate.commonalityMale)) || (pawn.gender == Gender.Female && (ate.commonalityFemale == -1f || Rand.Range(0, 100) < ate.commonalityFemale)) || pawn.gender == Gender.None)
                            pawn.story.traits.GainTrait(new Trait(TraitDef.Named(ate.defname), ate.degree, true));
        }

        public static void SkinColorPostfix(Pawn_StoryTracker __instance, ref Color __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null)
                __result = alienProps.alienPartGenerator.SkinColor(pawn);
        }

        public static void GenerateBodyTypePostfix(ref Pawn pawn)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null && !alienProps.alienPartGenerator.alienbodytypes.NullOrEmpty() && !alienProps.alienPartGenerator.alienbodytypes.Contains(pawn.story.bodyType))
                pawn.story.bodyType = alienProps.alienPartGenerator.alienbodytypes.RandomElement();
        }

        static FactionDef noHairFaction = new FactionDef() { hairTags = new List<string>() { "alienNoHair" } };
        static FactionDef hairFaction = new FactionDef();

        public static void RandomHairDefForPrefix(Pawn pawn, ref FactionDef factionType)
        {
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;
            if (alienProps != null)
            {
                if(!alienProps.HasHair)
                    factionType = noHairFaction;
                else if(!alienProps.hairTags.NullOrEmpty())
                {
                    hairFaction.hairTags = alienProps.hairTags;
                    factionType = hairFaction;
                }
            }
        }

        public static void GeneratePawnPrefix(ref PawnGenerationRequest request)
        {
            PawnKindDef kindDef = request.KindDef;
            if (Rand.Value <= 0.4f)
            {
                IEnumerable<ThingDef_AlienRace> comps = DefDatabase<ThingDef_AlienRace>.AllDefsListForReading;
                PawnKindEntry pk;
                if (request.KindDef == PawnKindDefOf.SpaceRefugee)
                {
                    if (comps.Where(r => !r.alienrefugeekinds.NullOrEmpty()).Select(r => r.alienrefugeekinds.RandomElement()).TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out pk))
                        kindDef = pk.kindDefs.RandomElement();
                }
                else if (request.KindDef == PawnKindDefOf.Slave)
                {
                    if (comps.Where(r => !r.alienslavekinds.NullOrEmpty()).Select(r => r.alienslavekinds.RandomElement()).TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out pk))
                        kindDef = pk.kindDefs.RandomElement();
                }
            }

            request = new PawnGenerationRequest(kindDef, request.Faction, request.Context, request.Map, request.ForceGenerateNewPawn, request.Newborn,
                request.AllowDead, request.AllowDead, request.CanGeneratePawnRelations, request.MustBeCapableOfViolence, request.ColonistRelationChanceFactor,
                request.ForceAddFreeWarmLayerIfNeeded, request.AllowGay, request.AllowFood, request.Validator, request.FixedBiologicalAge,
                request.FixedChronologicalAge, request.FixedGender, request.FixedMelanin, request.FixedLastName);
        }

        public static bool RenderPawnInternalPrefix(PawnRenderer __instance, Vector3 rootLoc, Quaternion quat, bool renderBody, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            ThingDef_AlienRace alienProps = pawn.def as ThingDef_AlienRace;


            if (alienProps == null)
                return true;

            if (!__instance.graphics.AllResolved)
            {
                __instance.graphics.ResolveAllGraphics();
            }
            Mesh mesh = null;

            if (renderBody)
            {
                Vector3 loc = rootLoc;
                loc.y += 0.005f;

                if (bodyDrawType == RotDrawMode.Rotting)
                    __instance.graphics.dessicatedGraphic.Draw(loc, bodyFacing, pawn);
                else
                {
                    mesh = (portrait ? alienProps.alienPartGenerator.bodyPortraitSet.MeshAt(bodyFacing) : alienProps.alienPartGenerator.bodySet.MeshAt(bodyFacing));
                }

                List<Material> list = __instance.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
                for (int i = 0; i < list.Count; i++)
                {
                    Material damagedMat = __instance.graphics.flasher.GetDamagedMat(list[i]);
                    GenDraw.DrawMeshNowOrLater(mesh, loc, quat, damagedMat, portrait);
                    loc.y += 0.005f;
                }
                if (bodyDrawType == RotDrawMode.Fresh)
                {
                    Vector3 drawLoc = rootLoc;
                    drawLoc.y += 0.02f;
                    Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>().RenderOverBody(drawLoc, mesh, quat, portrait);
                }
            }
            Vector3 vector = rootLoc;
            Vector3 a = rootLoc;
            if (bodyFacing != Rot4.North)
            {
                a.y += 0.03f;
                vector.y += 0.0249999985f;
            }
            else
            {
                a.y += 0.0249999985f;
                vector.y += 0.03f;
            }
            if (__instance.graphics.headGraphic != null)
            {
                Vector3 b = quat * __instance.BaseHeadOffsetAt(headFacing);
                Mesh mesh2 = (portrait ? alienProps.alienPartGenerator.headPortraitSet.MeshAt(headFacing) : alienProps.alienPartGenerator.headSet.MeshAt(headFacing));
                Material mat = __instance.graphics.HeadMatAt(headFacing, bodyDrawType);
                GenDraw.DrawMeshNowOrLater(mesh2, a + b, quat, mat, portrait);
                Vector3 loc2 = rootLoc + b;
                loc2.y += 0.035f;
                bool flag = false;
                Mesh mesh3 = (pawn.story.crownType == CrownType.Narrow ? (portrait ? alienProps.alienPartGenerator.hairPortraitSetNarrow : alienProps.alienPartGenerator.hairSetNarrow) : (portrait ? alienProps.alienPartGenerator.hairPortraitSetAverage : alienProps.alienPartGenerator.hairSetAverage)).MeshAt(headFacing);
                List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;
                for (int j = 0; j < apparelGraphics.Count; j++)
                {
                    if (apparelGraphics[j].sourceApparel.def.apparel.LastLayer == ApparelLayer.Overhead)
                    {
                        flag = true;
                        Material material = apparelGraphics[j].graphic.MatAt(bodyFacing, null);
                        material = __instance.graphics.flasher.GetDamagedMat(material);
                        GenDraw.DrawMeshNowOrLater(mesh3, loc2, quat, material, portrait);
                    }
                }
                if (!flag && bodyDrawType != RotDrawMode.Dessicated)
                {
                    Mesh mesh4 = (pawn.story.crownType == CrownType.Narrow ? (portrait ? alienProps.alienPartGenerator.hairPortraitSetNarrow : alienProps.alienPartGenerator.hairSetNarrow) : (portrait ? alienProps.alienPartGenerator.hairPortraitSetAverage : alienProps.alienPartGenerator.hairSetAverage)).MeshAt(headFacing);
                    Material mat2 = __instance.graphics.HairMatAt(headFacing);
                    GenDraw.DrawMeshNowOrLater(mesh4, loc2, quat, mat2, portrait);
                }
            }
            if (renderBody)
            {
                for (int k = 0; k < __instance.graphics.apparelGraphics.Count; k++)
                {
                    ApparelGraphicRecord apparelGraphicRecord = __instance.graphics.apparelGraphics[k];
                    if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayer.Shell)
                    {
                        Material material2 = apparelGraphicRecord.graphic.MatAt(bodyFacing, null);
                        material2 = __instance.graphics.flasher.GetDamagedMat(material2);
                        GenDraw.DrawMeshNowOrLater(mesh, vector, quat, material2, portrait);
                    }
                }
            }

            Traverse.Create(__instance).Method("DrawEquipment", new object[] { rootLoc });
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int l = 0; l < wornApparel.Count; l++)
                {
                    wornApparel[l].DrawWornExtras();
                }
            }
            Vector3 bodyLoc = rootLoc;
            bodyLoc.y += 0.0449999981f;
            Traverse.Create(__instance).Field("statusOverlays").GetValue<PawnHeadOverlays>().RenderStatusOverlays(bodyLoc, quat, portrait ? alienProps.alienPartGenerator.headPortraitSet.MeshAt(headFacing) : alienProps.alienPartGenerator.headSet.MeshAt(headFacing));
            return false;
        }
    }
}