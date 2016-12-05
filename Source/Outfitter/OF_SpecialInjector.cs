using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
// Toggle in Hospitality Properties
#if NoCCL
using Outfitter.NoCCL;
#else
using CommunityCoreLibrary;
#endif

namespace Outfitter
{

    public class OF_SpecialInjector : SpecialInjector
    {
        bool useApparelSense = false;

        private static Assembly Assembly
        {
            get
            {
                return Assembly.GetAssembly(typeof(OF_SpecialInjector));
            }
        }

        private static readonly BindingFlags[] bindingFlagCombos = {
            BindingFlags.Instance | BindingFlags.Public, BindingFlags.Static | BindingFlags.Public,
            BindingFlags.Instance | BindingFlags.NonPublic, BindingFlags.Static | BindingFlags.NonPublic,
        };

        #region ApparelSense
        // our root thingCategory def
        private ThingCategoryDef apparelRoot = ThingCategoryDefOf.Apparel;

        // create a category def and plop it into the defDB
        private ThingCategoryDef CreateCategory(string label, string type)
        {
            // create cat def
            ThingCategoryDef cat = new ThingCategoryDef();
            cat.parent = apparelRoot;
            cat.label = label;
            cat.defName = GetCatName(label, type);
            DefDatabase<ThingCategoryDef>.Add(cat);

            // don't forget to call the PostLoad() function, or you'll get swarmed in red... (ugh)
            cat.PostLoad();

            // update parent
            cat.parent.childCategories.Add(cat);

            // done!
            return cat;
        }

        // create a category def and plop it into the defDB
        private ThingCategoryDef CreateChildCategory(ThingCategoryDef thisRoot, string bodypart, string label, string type)
        {
            // create cat def
            ThingCategoryDef cat = new ThingCategoryDef();
            cat.parent = thisRoot;
            cat.label = label;
            cat.defName = GetChildCatName(bodypart, label, type);
            DefDatabase<ThingCategoryDef>.Add(cat);

            // don't forget to call the PostLoad() function, or you'll get swarmed in red... (ugh)
            cat.PostLoad();

            // update parent
            cat.parent.childCategories.Add(cat);

            // done!
            return cat;
        }

        // create a unique category name
        public string GetCatName(string label, string type)
        {
            return "ThingCategoryDef_Apparel_" + type + "_" + label;
        }
        // create a unique category name
        public string GetChildCatName(string cat, string label, string type)
        {
            return "ThingCategoryDef_Apparel_" + cat +"_" + type + "_" + label;
        }
        // exact copy of Verse.ThingCategoryNodeDatabase.SetNestLevelRecursive (Tynan, pls).
        private static void SetNestLevelRecursive(TreeNode_ThingCategory node, int nestDepth)
        {
            foreach (ThingCategoryDef current in node.catDef.childCategories)
            {
                current.treeNode.nestDepth = nestDepth;
                SetNestLevelRecursive(current.treeNode, nestDepth + 1);
            }
        }
        #endregion


        public override bool Inject()
        {

            #region Automatic hookup
            // Loop through all detour attributes and try to hook them up
            foreach (Type targetType in Assembly.GetTypes())
            {
                foreach (BindingFlags bindingFlags in bindingFlagCombos)
                {
                    foreach (MethodInfo targetMethod in targetType.GetMethods(bindingFlags))
                    {
                        foreach (DetourAttribute detour in targetMethod.GetCustomAttributes(typeof(DetourAttribute), true))
                        {
                            BindingFlags flags = detour.bindingFlags != default(BindingFlags) ? detour.bindingFlags : bindingFlags;
                            MethodInfo sourceMethod = detour.source.GetMethod(targetMethod.Name, flags);
                            if (sourceMethod == null)
                            {
                                Log.Error(string.Format("Outfitter :: Detours :: Can't find source method '{0} with bindingflags {1}", targetMethod.Name, flags));
                                return false;
                            }
                            if (!Detours.TryDetourFromTo(sourceMethod, targetMethod)) return false;
                        }
                    }
                }
            }
            #endregion

            /*
            MethodInfo coreMethod = typeof(JobGiver_OptimizeApparel).GetMethod("TryGiveJob", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo autoEquipMethod = typeof(Outfitter_JobGiver_OptimizeApparel).GetMethod("TryGiveJob", BindingFlags.Instance | BindingFlags.NonPublic);

            if (!Detours.TryDetourFromTo(coreMethod, autoEquipMethod))
                Log.Error("Could not Detour AutoEquip.");
            */

            // inject ITab into all humanlikes
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.category == ThingCategory.Pawn && td.race.Humanlike))
            {
                if (def.inspectorTabs == null || def.inspectorTabs.Count == 0)
                {
                    def.inspectorTabs = new List<Type>();
                    def.inspectorTabsResolved = new List<ITab>();
                }
                if (def.inspectorTabs.Contains(typeof(ITab_Pawn_Outfitter)))
                {
                    return false;
                }

                def.inspectorTabs.Add(typeof(ITab_Pawn_Outfitter));
                def.inspectorTabsResolved.Add(ITabManager.GetSharedInstance(typeof(ITab_Pawn_Outfitter)));
            }

            GameObject initializer = new GameObject("OutfitterMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            UnityEngine.Object.DontDestroyOnLoad(initializer);

            if (useApparelSense)
            {
                // get a list of all apparel in the game
                List<ThingDef> allApparel = DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.IsApparel).ToList();

                // detach all existing categories under apparel
                foreach (ThingCategoryDef cat in apparelRoot.childCategories)
                {
                    cat.parent = null;
                }
                apparelRoot.childCategories = new List<ThingCategoryDef>();

                // loop over all apparel, adding categories where appropriate.
                foreach (ThingDef thing in allApparel)
                {
                    // create list of categories on thing if necessary (shouldn't ever be, but what the heck)
                    if (thing.thingCategories.NullOrEmpty())
                    {
                        thing.thingCategories = new List<ThingCategoryDef>();
                    }

                    // remove existing categories on thing
                    foreach (ThingCategoryDef cat in thing.thingCategories)
                    {
                        cat.childThingDefs.Remove(thing);
                    }

                    // add in new categories
                    ApparelProperties apparel = thing.apparel;

                    // categories based on bodyparts
                    foreach (BodyPartGroupDef bodyPart in apparel.bodyPartGroups)
                    {
                        // get or create category
                        ThingCategoryDef cat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(GetCatName(bodyPart.label, "BP"));
                        if (cat == null)
                        {
                            cat = CreateCategory(bodyPart.label, "BP");
                        }

                        foreach (ApparelLayer layer in apparel.layers)
                        {
                            // get or create category
                            ThingCategoryDef childCat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(GetChildCatName(bodyPart.label, layer.ToString(), "CC"));
                            if (childCat == null)
                            {
                                childCat = CreateChildCategory(cat, bodyPart.label, layer.ToString(), "CC");
                            }
                            // add category to thing, and thing to category. (Tynan, pls.)
                            thing.thingCategories.Add(childCat);
                            childCat.childThingDefs.Add(thing);
                        }


                    }

                    // categories based on tag (too messy)


                    //// categories based on tag (too messy)
                    //foreach ( string tag in apparel.tags )
                    //{
                    //    // get or create category
                    //    ThingCategoryDef cat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail( GetCatName( tag, "BP" ) );
                    //    if( cat == null )
                    //    {
                    //        cat = CreateCategory( tag, "BP" );
                    //    }

                    //    // add category to thing, and thing to category. (Tynan, pls.)
                    //    thing.thingCategories.Add( cat );
                    //    cat.childThingDefs.Add( thing );
                    //}
                }

                // set nest levels on new categories
                SetNestLevelRecursive(apparelRoot.treeNode, apparelRoot.treeNode.nestDepth);
            }

            return true;
        }
    }
}
