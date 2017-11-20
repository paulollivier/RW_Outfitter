namespace Outfitter
{
    using System;
    using System.Linq;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    [StaticConstructorOnStartup]
    public static class TabsPatch
    {
        #region Public Methods

        // Resizes the tabs if more than 6 visible
        public static bool DoTabs_Prefix(IInspectPane pane)
        {
            try
            {
                int count = pane.CurTabs.Count(x => x.IsVisible);
                if (count > 6)
                {
                    tabWidth = PaneWidth / count;
                }
                else
                {
                    tabWidth = 72f;
                }

                float y = pane.PaneTopY - TabHeight;
                float num = PaneWidth - tabWidth;
                float width = 0f;
                bool flag = false;

                foreach (InspectTabBase current in pane.CurTabs)
                {
                    if (current.IsVisible)
                    {
                        Rect rect = new Rect(num, y, tabWidth, TabHeight);
                        width = num;
                        Text.Font = GameFont.Small;
                        if (Widgets.ButtonText(rect, current.labelKey.Translate()))
                        {
                            InterfaceToggleTab(current, pane);
                        }

                        bool flag2 = current.GetType() == pane.OpenTabType;
                        if (!flag2 && !current.TutorHighlightTagClosed.NullOrEmpty())
                        {
                            UIHighlighter.HighlightOpportunity(rect, current.TutorHighlightTagClosed);
                        }

                        if (flag2)
                        {
                            current.DoTabGUI();
                            pane.RecentHeight = 700f;
                            flag = true;
                        }

                        num -= tabWidth;
                    }
                }

                if (flag)
                {
                    GUI.DrawTexture(new Rect(0f, y, width, TabHeight), InspectTabButtonFillTex);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(ex.ToString(), 742783);
            }

            return false;
        }

        #endregion Public Methods

        #region Private Fields

        private const float PaneWidth = 432f;

        private const float TabHeight = 30f;

        private static readonly Texture2D InspectTabButtonFillTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.07450981f, 0.08627451f, 0.105882354f, 1f));

        private static float tabWidth = 72f;

        #endregion Private Fields

        #region Private Methods

        private static void InterfaceToggleTab(InspectTabBase tab, IInspectPane pane)
        {
            if (TutorSystem.TutorialMode && !IsOpen(tab, pane)
                && !TutorSystem.AllowAction("ITab-" + tab.tutorTag + "-Open"))
            {
                return;
            }

            ToggleTab(tab, pane);
        }

        private static bool IsOpen(InspectTabBase tab, IInspectPane pane)
        {
            return tab.GetType() == pane.OpenTabType;
        }

        private static void ToggleTab([NotNull] InspectTabBase tab, IInspectPane pane)
        {
            if (IsOpen(tab, pane) || tab == null && pane.OpenTabType == null)
            {
                pane.OpenTabType = null;
                SoundDefOf.TabClose.PlayOneShotOnCamera(null);
            }
            else
            {
                tab.OnOpen();
                pane.OpenTabType = tab.GetType();
                SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
            }
        }

        #endregion Private Methods
    }
}