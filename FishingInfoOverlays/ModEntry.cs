﻿using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;

namespace StardewMods
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        ITranslationHelper translate;
        private bool dayStarted;    //world fish preview data 
        private Farmer who;
        private Dictionary<string, string> locationData;
        private Dictionary<int, string> fishData;
        private Texture2D background;
        private int totalPlayersOnThisPC;


        private readonly PerScreen<List<int>> fishHere = new PerScreen<List<int>>();//per screen data
        private readonly PerScreen<Dictionary<int, int>> fishChances = new PerScreen<Dictionary<int, int>>();
        private readonly PerScreen<Dictionary<int, int>> fishChancesSlow = new PerScreen<Dictionary<int, int>>();
        private readonly PerScreen<int> fishChancesModulo = new PerScreen<int>();
        private readonly PerScreen<string> oldLoc = new PerScreen<string>();
        private readonly PerScreen<Item> oldTool = new PerScreen<Item>();
        private readonly PerScreen<int> oldBait = new PerScreen<int>();
        private readonly PerScreen<int> oldZone = new PerScreen<int>();
        private readonly PerScreen<int> oldTime = new PerScreen<int>();


        private readonly PerScreen<bool> isMinigame = new PerScreen<bool>();    //minigame fish preview data, Reflection
        private readonly PerScreen<int> miniFish = new PerScreen<int>();
        private readonly PerScreen<float> miniFishPos = new PerScreen<float>();
        private readonly PerScreen<int> miniXPositionOnScreen = new PerScreen<int>();
        private readonly PerScreen<int> miniYPositionOnScreen = new PerScreen<int>();
        private readonly PerScreen<Vector2> miniFishShake = new PerScreen<Vector2>();
        private readonly PerScreen<Vector2> miniEverythingShake = new PerScreen<Vector2>();
        private readonly PerScreen<Vector2> miniBarShake = new PerScreen<Vector2>();
        private readonly PerScreen<Vector2> miniTreasureShake = new PerScreen<Vector2>();
        private readonly PerScreen<float> miniScale = new PerScreen<float>();
        private readonly PerScreen<bool> miniBobberInBar = new PerScreen<bool>();
        private readonly PerScreen<float> miniBobberBarPos = new PerScreen<float>();
        private readonly PerScreen<float> miniBobberBarHeight = new PerScreen<float>();
        private readonly PerScreen<float> miniTreasurePosition = new PerScreen<float>();
        private readonly PerScreen<float> miniTreasureScale = new PerScreen<float>();
        private readonly PerScreen<float> miniTreasureCatchLevel = new PerScreen<float>();
        private readonly PerScreen<bool> miniTreasureCaught = new PerScreen<bool>();


        private ModConfig config;   //config values
        private int miniMode = 0;
        private bool barCrabEnabled = true;
        private Vector2 barPosition;
        private int iconMode = 0;
        private float barScale = 0;
        private int maxIcons = 0;
        private int maxIconsPerRow = 0;
        private int backgroundMode = 0;
        private int extraCheckFrequency = 0;
        private int scanRadius = 0;
        private bool showTackles = true;
        private bool showPercentages = true;
        private int sortMode = 0;
        private bool uncaughtDark = true;




        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            translate = helper.Translation;

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Display.RenderedHud += this.RenderedHud;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderMenu;
            helper.Events.GameLoop.GameLaunched += this.GenericModConfigMenuIntegration;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        }

        private void GenericModConfigMenuIntegration(object sender, GameLaunchedEventArgs e)     //Generic Mod Config Menu API
        {
            var GenericMC = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (GenericMC != null)
            {
                GenericMC.RegisterModConfig(ModManifest, () => config = new ModConfig(), () => Helper.WriteConfig(config));
                GenericMC.SetDefaultIngameOptinValue(ModManifest, true);
                GenericMC.RegisterLabel(ModManifest, translate.Get("GenericMC.barLabel"), ""); //All of these strings are stored in the traslation files.
                GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.barDescription"));
                GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.barDescription2"));
                if (Constants.TargetPlatform != GamePlatform.Android)
                {
                    GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.barDescriptionPC"));
                    GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.barDescriptionPC2"));
                }
                else GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.barDescriptionOther"));

                GenericMC.RegisterChoiceOption(ModManifest, translate.Get("GenericMC.barIconMode"), translate.Get("GenericMC.barIconModeDesc"),
                    () => (config.BarIconMode == 0) ? translate.Get("GenericMC.barIconModeHor") : (config.BarIconMode == 1) ? translate.Get("GenericMC.barIconModeVert") : (config.BarIconMode == 2) ? translate.Get("GenericMC.barIconModeVertText") : translate.Get("GenericMC.Disabled"),
                    (string val) => config.BarIconMode = Int32.Parse((val.Equals(translate.Get("GenericMC.barIconModeHor"), StringComparison.Ordinal)) ? "0" : (val.Equals(translate.Get("GenericMC.barIconModeVert"), StringComparison.Ordinal)) ? "1" : (!val.Equals(translate.Get("GenericMC.Disabled"), StringComparison.Ordinal)) ? "2" : "3"),
                    new string[] { translate.Get("GenericMC.barIconModeHor"), translate.Get("GenericMC.barIconModeVert"), translate.Get("GenericMC.barIconModeVertText"), translate.Get("GenericMC.Disabled") });//small 'hack' so options appear as name strings, while config.json stores them as integers
                
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barPosX"), translate.Get("GenericMC.barPosXDesc"),
                     () => config.BarTopLeftLocationX, (int val) => config.BarTopLeftLocationX = Math.Max(0, val));
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barPosY"), translate.Get("GenericMC.barPosYDesc"),
                    () => config.BarTopLeftLocationY, (int val) => config.BarTopLeftLocationY = Math.Max(0, val));
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barScale"), translate.Get("GenericMC.barScaleDesc"),
                    () => (float)config.BarScale, (float val) => config.BarScale = Math.Min(10, Math.Max(0.1f, val)));
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barMaxIcons"), translate.Get("GenericMC.barMaxIconsDesc"),
                   () => config.BarMaxIcons, (int val) => config.BarMaxIcons = (int)Math.Min(500, Math.Max(4, val)));
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barMaxIconsPerRow"), translate.Get("GenericMC.barMaxIconsPerRowDesc"),
                    () => config.BarMaxIconsPerRow, (int val) => config.BarMaxIconsPerRow = (int)Math.Min(500, Math.Max(4, val)));

                GenericMC.RegisterChoiceOption(ModManifest, translate.Get("GenericMC.barBackgroundMode"), translate.Get("GenericMC.barBackgroundModeDesc"),
                    () => (config.BarBackgroundMode == 0) ? translate.Get("GenericMC.barBackgroundModeCircles") : (config.BarBackgroundMode == 1) ? translate.Get("GenericMC.barBackgroundModeRect") : translate.Get("GenericMC.Disabled"),
                    (string val) => config.BarBackgroundMode = Int32.Parse((val.Equals(translate.Get("GenericMC.barBackgroundModeCircles"), StringComparison.Ordinal)) ? "0" : (val.Equals(translate.Get("GenericMC.barBackgroundModeRect"), StringComparison.Ordinal)) ? "1" : "2"),
                    new string[] { translate.Get("GenericMC.barBackgroundModeCircles"), translate.Get("GenericMC.barBackgroundModeRect"), translate.Get("GenericMC.Disabled") });

                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barShowBaitTackle"), translate.Get("GenericMC.barShowBaitTackleDesc"),
                    () => config.BarShowBaitAndTackleInfo, (bool val) => config.BarShowBaitAndTackleInfo = val);
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barShowPercentages"), translate.Get("GenericMC.barShowPercentagesDesc"),
                    () => config.BarShowPercentages, (bool val) => config.BarShowPercentages = val);

                GenericMC.RegisterChoiceOption(ModManifest, translate.Get("GenericMC.barSortMode"), translate.Get("GenericMC.barSortModeDesc"),
                    () => (config.BarSortMode == 0) ? translate.Get("GenericMC.barSortModeName") : (config.BarSortMode == 1) ? translate.Get("GenericMC.barSortModeChance") : translate.Get("GenericMC.Disabled"),
                    (string val) => config.BarSortMode = Int32.Parse((val.Equals(translate.Get("GenericMC.barSortModeName"), StringComparison.Ordinal)) ? "0" : (val.Equals(translate.Get("GenericMC.barSortModeChance"), StringComparison.Ordinal)) ? "1" : "2"),
                    new string[] { translate.Get("GenericMC.barSortModeName"), translate.Get("GenericMC.barSortModeChance"), translate.Get("GenericMC.Disabled") });

                GenericMC.RegisterClampedOption(ModManifest, translate.Get("GenericMC.barExtraCheckFrequency"), translate.Get("GenericMC.barExtraCheckFrequencyDesc"),
                    () => config.BarExtraCheckFrequency, (int val) => config.BarExtraCheckFrequency = val, 20, 220);
                GenericMC.RegisterClampedOption(ModManifest, translate.Get("GenericMC.barScanRadius"), translate.Get("GenericMC.barScanRadiusDesc"),
                    () => config.BarScanRadius, (int val) => config.BarScanRadius = val, 0, 50);
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barCrabPotEnabled"), translate.Get("GenericMC.barCrabPotEnabledDesc"),
                    () => config.BarCrabPotEnabled, (bool val) => config.BarCrabPotEnabled = val);
                GenericMC.RegisterSimpleOption(ModManifest, translate.Get("GenericMC.barUncaughtDarker"), translate.Get("GenericMC.barUncaughtDarkerDesc"),
                    () => config.UncaughtFishAreDark, (bool val) => config.UncaughtFishAreDark = val);

                GenericMC.RegisterLabel(ModManifest, translate.Get("GenericMC.MinigameLabel"), "");
                GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.MinigameDescription"));
                GenericMC.RegisterParagraph(ModManifest, translate.Get("GenericMC.MinigameDescription2"));
                GenericMC.RegisterChoiceOption(ModManifest, translate.Get("GenericMC.MinigameMode"), translate.Get("GenericMC.MinigameModeDesc"),
                    () => (config.MinigamePreviewMode == 0) ? translate.Get("GenericMC.MinigameModeFull") : (config.MinigamePreviewMode == 1) ? translate.Get("GenericMC.MinigameModeSimple") : (config.MinigamePreviewMode == 2) ? translate.Get("GenericMC.MinigameModeBarOnly") : translate.Get("GenericMC.Disabled"),
                    (string val) => config.MinigamePreviewMode = Int32.Parse((val.Equals(translate.Get("GenericMC.MinigameModeFull"), StringComparison.Ordinal)) ? "0" : (val.Equals(translate.Get("GenericMC.MinigameModeSimple"), StringComparison.Ordinal)) ? "1" : (val.Equals(translate.Get("GenericMC.MinigameModeBarOnly"), StringComparison.Ordinal)) ? "2" : "3"),
                    new string[] { translate.Get("GenericMC.MinigameModeFull"), translate.Get("GenericMC.MinigameModeSimple"), translate.Get("GenericMC.MinigameModeBarOnly"), translate.Get("GenericMC.Disabled") });
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !(e.Button == SButton.F5)) return; // ignore if player hasn't loaded a save yet
            config = Helper.ReadConfig<ModConfig>();
            translate = Helper.Translation;
            this.UpdateConfig();
        }


        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            dayStarted = false;
            if (!Context.IsSplitScreen) this.UpdateConfig();
            dayStarted = true;
        }


        /*  Travel direction, maxIcons + maxIconsPerRow DONE
         *  make config update on day start + f5 only: update function. DONE
         *  Tackle + bait preview with values? DONE
         *  2ndary check for modded fish: Bad performance, setting is Check Frequency: 1-300 slider? DONE
         *  
         *  Dark preview (???) if fish not caught. DONE
         *  Crab Pot preview? DONE
         *  Minigame: Preview, if not caught dark. Full, simple, just on preview, off. DONE
         */

        private void RenderedHud(object sender, RenderedHudEventArgs e)
        {
            totalPlayersOnThisPC = 1;
            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsSplitScreen) totalPlayersOnThisPC++;
            }

            who = Game1.player;
            if (!dayStarted || Game1.eventUp || who.CurrentItem == null ||
                !((who.CurrentItem is FishingRod) || (who.CurrentItem.Name.Equals("Crab Pot", StringComparison.Ordinal) && barCrabEnabled)))
            {
                oldTool.Value = null;
                return;//code stop conditions
            }

            if (Game1.player.CurrentItem is FishingRod)  //dummy workaround for preventing player from getting special items
            {
                who = new Farmer();
                who.mailReceived.CopyFrom(Game1.player.mailReceived);
                who.mailReceived.Add("CalderaPainting");
                who.currentLocation = Game1.player.currentLocation;
                who.setTileLocation(Game1.player.getTileLocation());
                who.FishingLevel = Game1.player.FishingLevel;
                who.CurrentTool = Game1.player.CurrentTool;
                who.LuckLevel = Game1.player.LuckLevel;
                foreach (var item in Game1.player.fishCaught) who.fishCaught.Add(item);
                who.secretNotesSeen.CopyFrom(Game1.player.secretNotesSeen);
            }

            SpriteFont font = Game1.smallFont;                                                          //UI INIT
            Rectangle source = GameLocation.getSourceRectForObject(who.CurrentItem.ParentSheetIndex);      //for average icon size
            SpriteBatch batch = Game1.spriteBatch;

            batch.End();    //stop current UI drawing and start mode where where layers work from 0f-1f
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            //MINIGAME PREVIEW
            if (isMinigame.Value && miniMode < 2 && miniScale.Value == 1f)//scale == 1f when moving elements appear
            {
                if (miniMode == 0) //Full minigame
                {
                    //rod+bar textture cut to only cover the minigame bar
                    batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 126, miniYPositionOnScreen.Value + 292) + miniEverythingShake.Value),
                        new Rectangle(658, 1998, 15, 149), Color.White * miniScale.Value, 0f, new Vector2(18.5f, 74f) * miniScale.Value, Utility.ModifyCoordinateForUIScale(4f * miniScale.Value), SpriteEffects.None, 0.01f);

                    //green moving bar player controls
                    batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 64, miniYPositionOnScreen.Value + 12 + (int)miniBobberBarPos.Value) + miniBarShake.Value + miniEverythingShake.Value),
                        new Rectangle(682, 2078, 9, 2), miniBobberInBar.Value ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(4f), SpriteEffects.None, 0.89f);
                    batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 64, miniYPositionOnScreen.Value + 12 + (int)miniBobberBarPos.Value + 8) + miniBarShake.Value + miniEverythingShake.Value),
                        new Rectangle(682, 2081, 9, 1), miniBobberInBar.Value ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, Utility.ModifyCoordinatesForUIScale(new Vector2(4f, miniBobberBarHeight.Value - 16)), SpriteEffects.None, 0.89f);
                    batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 64, miniYPositionOnScreen.Value + 12 + (int)miniBobberBarPos.Value + miniBobberBarHeight.Value - 8) + miniBarShake.Value + miniEverythingShake.Value),
                        new Rectangle(682, 2085, 9, 2), miniBobberInBar.Value ? Color.White : (Color.White * 0.25f * ((float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0), 2) + 2f)), 0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(4f), SpriteEffects.None, 0.89f);

                    //treasure
                    batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 64 + 18, (float)(miniYPositionOnScreen.Value + 12 + 24) + miniTreasurePosition.Value) + miniTreasureShake.Value + miniEverythingShake.Value),
                        new Rectangle(638, 1865, 20, 24), Color.White, 0f, new Vector2(10f, 10f), Utility.ModifyCoordinateForUIScale(2f * miniTreasureScale.Value), SpriteEffects.None, 0.9f);
                    if (miniTreasureCatchLevel.Value > 0f && !miniTreasureCaught.Value)//treasure progress
                    {
                        batch.Draw(Game1.staminaRect, new Rectangle((int)Utility.ModifyCoordinateForUIScale(miniXPositionOnScreen.Value + 64), (int)Utility.ModifyCoordinateForUIScale(miniYPositionOnScreen.Value + 12 + (int)miniTreasurePosition.Value), (int)Utility.ModifyCoordinateForUIScale(40), (int)Utility.ModifyCoordinateForUIScale(8)), null, Color.DimGray * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                        batch.Draw(Game1.staminaRect, new Rectangle((int)Utility.ModifyCoordinateForUIScale(miniXPositionOnScreen.Value + 64), (int)Utility.ModifyCoordinateForUIScale(miniYPositionOnScreen.Value + 12 + (int)miniTreasurePosition.Value), (int)Utility.ModifyCoordinateForUIScale((miniTreasureCatchLevel.Value * 40f)), (int)Utility.ModifyCoordinateForUIScale(8)), null, Color.Orange, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                    }
                }
                else batch.Draw(Game1.mouseCursors, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 82, (miniYPositionOnScreen.Value + 36) + miniFishPos.Value) + miniFishShake.Value + miniEverythingShake.Value),
                    new Rectangle(614 + (FishingRod.isFishBossFish(miniFish.Value) ? 20 : 0), 1840, 20, 20), Color.Black, 0f, new Vector2(10f, 10f),
                    Utility.ModifyCoordinateForUIScale(2.05f), SpriteEffects.None, 0.9f);//Simple minigame shadow fish

                //fish
                source = GameLocation.getSourceRectForObject(miniFish.Value);
                batch.Draw(Game1.objectSpriteSheet, Utility.ModifyCoordinatesForUIScale(new Vector2(miniXPositionOnScreen.Value + 82, (miniYPositionOnScreen.Value + 36) + miniFishPos.Value) + miniFishShake.Value + miniEverythingShake.Value),
                    source, (!uncaughtDark || who.fishCaught.ContainsKey(miniFish.Value)) ? Color.White : Color.DarkSlateGray, 0f, new Vector2(9.5f, 9f),
                    Utility.ModifyCoordinateForUIScale(3f), SpriteEffects.FlipHorizontally, 1f);
            }



            if (iconMode != 3)
            {
                float iconScale = Game1.pixelZoom / 2f * barScale;
                int iconCount = 0;
                float boxWidth = 0;
                float boxHeight = 0;
                Vector2 boxTopLeft = barPosition;
                Vector2 boxBottomLeft = barPosition;

                if (showTackles && who.CurrentItem is FishingRod)    //BAIT AND TACKLE (BOBBERS) PREVIEW
                {
                    int bait = (who.CurrentItem as FishingRod).getBaitAttachmentIndex();
                    int tackle = (who.CurrentItem as FishingRod).getBobberAttachmentIndex();
                    if (bait > -1)
                    {
                        source = GameLocation.getSourceRectForObject(bait);
                        if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);

                        int baitCount = (who.CurrentItem as FishingRod).attachments[0].Stack;
                        batch.Draw(Game1.objectSpriteSheet, boxBottomLeft, source, Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.9f);
                        Utility.drawTinyDigits(baitCount, batch, boxBottomLeft + new Vector2((source.Width * iconScale) - Utility.getWidthOfTinyDigitString(baitCount, 2f * barScale), (showPercentages ? 26 : 18) * barScale), 2f * barScale, 1f, Color.AntiqueWhite);

                        if (iconMode == 1) boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                        else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                        iconCount++;
                    }
                    if (tackle > -1)
                    {
                        source = GameLocation.getSourceRectForObject(tackle);
                        if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);

                        int tackleCount = FishingRod.maxTackleUses - (who.CurrentItem as FishingRod).attachments[1].uses;
                        batch.Draw(Game1.objectSpriteSheet, boxBottomLeft, source, Color.White, 0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.9f);
                        Utility.drawTinyDigits(tackleCount, batch, boxBottomLeft + new Vector2((source.Width * iconScale) - Utility.getWidthOfTinyDigitString(tackleCount, 2f * barScale), (showPercentages ? 26 : 18) * barScale), 2f * barScale, 1f, Color.AntiqueWhite);

                        if (iconMode == 1) boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                        else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                        iconCount++;
                    }
                    if (iconMode == 2 && (bait + tackle) > -1)
                    {
                        boxBottomLeft = boxTopLeft + new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                        boxWidth = (iconCount * source.Width * iconScale) + boxTopLeft.X;
                        boxHeight += (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0);
                        if (bait > 0 && tackle > 0) iconCount--;
                    }
                }


                bool foundWater = false;
                if (who.currentLocation.canFishHere())      //water nearby check
                {
                    if (scanRadius > 0)
                    {
                        Vector2 scanTopLeft = who.getTileLocation() - new Vector2(scanRadius + 1);
                        Vector2 scanBottomRight = who.getTileLocation() + new Vector2(scanRadius + 2);
                        for (int x = (int)scanTopLeft.X; x < (int)scanBottomRight.X; x++)
                        {
                            for (int y = (int)scanTopLeft.Y; y < (int)scanBottomRight.Y; y++)
                            {
                                if (who.currentLocation.isTileFishable(x, y))
                                {
                                    foundWater = true;
                                    break;
                                }
                            }
                            if (foundWater) break;
                        }
                    }
                    else foundWater = true;
                }

                if (foundWater)
                {
                    string locationName = who.currentLocation.Name;    //LOCATION FISH PREVIEW                 //this.Monitor.Log("\n", LogLevel.Debug);
                    if (who.currentLocation is Railroad || who.currentLocation is IslandFarmCave || (who.currentLocation is MineShaft && who.CurrentItem.Name.Equals("Crab Pot", StringComparison.Ordinal)))//crab pot
                    {
                        if (who.currentLocation is MineShaft)
                        {
                            string warning = translate.Get("Bar.CrabMineWarning");
                            batch.DrawString(font, warning, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Black, 0f, new Vector2(1, -2), 1f * barScale, SpriteEffects.None, 0.9f); //textbg
                            batch.DrawString(font, warning, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Black, 0f, new Vector2(-1, -4), 1f * barScale, SpriteEffects.None, 0.9f); //textbg
                            batch.DrawString(font, warning, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Red, 0f, new Vector2(0, -3), 1f * barScale, SpriteEffects.None, 1f); //text
                        }
                        batch.End();
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                        oldTool.Value = null;
                        return;
                    }

                    if (who.CurrentItem is FishingRod)
                    {
                        if (!isMinigame.Value)//don't reset main list while minigame to prevent lag
                        {
                            if (oldTime.Value != Game1.timeOfDay || oldTool.Value != who.CurrentItem || !oldLoc.Value.Equals(who.currentLocation.Name, StringComparison.Ordinal) || oldZone.Value != who.currentLocation.getFishingLocation(who.getTileLocation()) || oldBait.Value != (who.CurrentItem as FishingRod).getBaitAttachmentIndex())
                            {
                                oldLoc.Value = who.currentLocation.Name;
                                oldBait.Value = (who.CurrentItem as FishingRod).getBaitAttachmentIndex();
                                oldTime.Value = Game1.timeOfDay;
                                oldZone.Value = who.currentLocation.getFishingLocation(who.getTileLocation());
                                fishHere.Value = new List<int> { 168 };
                                fishChances.Value = new Dictionary<int, int> { { -1, 0 }, { 168, 0 } };
                                fishChancesSlow.Value = new Dictionary<int, int>();
                                fishChancesModulo.Value = 1;

                                AddGenericFishToList(locationName, who.currentLocation.getFishingLocation(who.getTileLocation()));
                            }
                        }
                        AddFishToListDynamic();
                    }
                    else AddCrabPotFish();
                    //for (int i = 0; i < 20; i++)    //TEST ITEM INSERT
                    //{
                    //    fishHere.Add(100 + i);
                    //}

                    foreach (var fish in fishHere.Value)
                    {
                        if (iconCount < maxIcons)
                        {
                            bool caught = (!uncaughtDark || who.fishCaught.ContainsKey(fish));
                            if (fish == 168) caught = true;

                            iconCount++;
                            string fishNameLocalized = "???";

                            if (new StardewValley.Object(fish, 1).Name.Equals("Error Item", StringComparison.Ordinal))  //Furniture
                            {
                                if (caught) fishNameLocalized = new StardewValley.Objects.Furniture(fish, Vector2.Zero).DisplayName;

                                batch.Draw(StardewValley.Objects.Furniture.furnitureTexture, boxBottomLeft, new StardewValley.Objects.Furniture(fish, Vector2.Zero).defaultSourceRect,
                                    (caught) ? Color.White : Color.DarkSlateGray, 0f, Vector2.Zero, 0.95f * barScale, SpriteEffects.None, 0.98f);//icon
                            }
                            else                                                                                        //Item
                            {
                                if (caught) fishNameLocalized = new StardewValley.Object(fish, 1).DisplayName;

                                source = GameLocation.getSourceRectForObject(fish);
                                if (fish == 168) batch.Draw(Game1.objectSpriteSheet, boxBottomLeft + new Vector2(2 * barScale, -5 * barScale), source, (caught) ? Color.White : Color.DarkSlateGray,
                                    0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.98f);//icon trash
                                else batch.Draw(Game1.objectSpriteSheet, boxBottomLeft, source, (caught) ? Color.White : Color.DarkSlateGray,
                                    0f, Vector2.Zero, 1.9f * barScale, SpriteEffects.None, 0.98f);//icon
                            }

                            if (showPercentages)
                            {
                                int percent = 1;
                                if (fishChancesSlow.Value.ContainsKey(fish)) percent = (int)Math.Round((float)fishChancesSlow.Value[fish] / (float)fishChancesSlow.Value[-1] * 100);
                                batch.DrawString(font, percent + "%", boxBottomLeft + new Vector2((source.Width * iconScale) - ((font.MeasureString(percent + "%").X + 8) * 0.5f * barScale), 28 * barScale),
                                    (caught) ? Color.White : Color.DarkGray, 0f, Vector2.Zero, 0.5f * barScale, SpriteEffects.None, 1f);//%
                            }

                            if (fish == miniFish.Value && miniMode < 3) batch.Draw(background, new Rectangle((int)boxBottomLeft.X - 1, (int)boxBottomLeft.Y - 1, (int)(source.Width * iconScale) + 1, (int)((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0) + 1)),
                                null, Color.GreenYellow, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);//minigame outline

                            if (backgroundMode == 0) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);


                            if (iconMode == 0)      //Horizontal Preview
                            {
                                if (iconCount % maxIconsPerRow == 0) boxBottomLeft = new Vector2(boxTopLeft.X, boxBottomLeft.Y + (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)); //row switch
                                else boxBottomLeft += new Vector2(source.Width * iconScale, 0);
                            }
                            else                    //Vertical Preview
                            {
                                if (iconMode == 2)  // + text
                                {
                                    if (backgroundMode == 0)
                                    {
                                        batch.DrawString(font, fishNameLocalized, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Black, 0f, new Vector2(1, -2), 1f * barScale, SpriteEffects.None, 0.9f); //textbg
                                        batch.DrawString(font, fishNameLocalized, boxBottomLeft + new Vector2(source.Width * iconScale, 0), Color.Black, 0f, new Vector2(-1, -4), 1f * barScale, SpriteEffects.None, 0.9f); //textbg
                                    }
                                    batch.DrawString(font, fishNameLocalized, boxBottomLeft + new Vector2(source.Width * iconScale, 0), (caught) ? Color.White : Color.DarkGray, 0f, new Vector2(0, -3), 1f * barScale, SpriteEffects.None, 0.98f); //text
                                    boxWidth = Math.Max(boxWidth, boxBottomLeft.X + (font.MeasureString(fishNameLocalized).X * barScale) + (source.Width * iconScale));
                                }

                                if (iconCount % maxIconsPerRow == 0) //row switch
                                {
                                    if (iconMode == 2) boxBottomLeft = new Vector2(boxWidth + (20 * barScale), boxTopLeft.Y);
                                    else boxBottomLeft = new Vector2(boxBottomLeft.X + (source.Width * iconScale), boxTopLeft.Y);
                                }
                                else boxBottomLeft += new Vector2(0, (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0));
                                if (iconMode == 2 && iconCount <= maxIconsPerRow) boxHeight += (source.Width * iconScale) + (showPercentages ? 10 * barScale : 0);
                            }
                        }
                    }
                    if (backgroundMode == 1) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);
                }
                else if (backgroundMode == 1) AddBackground(batch, boxTopLeft, boxBottomLeft, iconCount, source, iconScale, boxWidth, boxHeight);
            }

            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            oldTool.Value = who.CurrentItem;
        }



        private void OnMenuChanged(object sender, MenuChangedEventArgs e)   //Minigame data
        {
            if (e.NewMenu is BobberBar) isMinigame.Value = true;
            else
            {
                isMinigame.Value = false;
                miniFish.Value = -1;
                oldTime.Value = 0;
            }
        }
        private void OnRenderMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if ((Game1.activeClickableMenu is BobberBar bar) && isMinigame.Value)
            {
                miniFish.Value = Helper.Reflection.GetField<int>(bar, "whichFish").GetValue();
                if (miniMode < 2)
                {
                    miniScale.Value = Helper.Reflection.GetField<float>(bar, "scale").GetValue();
                    miniFishPos.Value = Helper.Reflection.GetField<Single>(bar, "bobberPosition").GetValue();
                    miniXPositionOnScreen.Value = Helper.Reflection.GetField<int>(bar, "xPositionOnScreen").GetValue();
                    miniYPositionOnScreen.Value = Helper.Reflection.GetField<int>(bar, "yPositionOnScreen").GetValue();
                    miniFishShake.Value = Helper.Reflection.GetField<Vector2>(bar, "fishShake").GetValue();
                    miniEverythingShake.Value = Helper.Reflection.GetField<Vector2>(bar, "everythingShake").GetValue();
                }
                if (miniMode == 0)
                {
                    miniBarShake.Value = Helper.Reflection.GetField<Vector2>(bar, "barShake").GetValue();
                    miniTreasureShake.Value = Helper.Reflection.GetField<Vector2>(bar, "treasureShake").GetValue();
                    miniBobberInBar.Value = Helper.Reflection.GetField<bool>(bar, "bobberInBar").GetValue();
                    miniBobberBarPos.Value = Helper.Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                    miniBobberBarHeight.Value = Helper.Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
                    miniTreasurePosition.Value = Helper.Reflection.GetField<float>(bar, "treasurePosition").GetValue();
                    miniTreasureScale.Value = Helper.Reflection.GetField<float>(bar, "treasureScale").GetValue();
                    miniTreasureCatchLevel.Value = Helper.Reflection.GetField<float>(bar, "treasureCatchLevel").GetValue();
                    miniTreasureCaught.Value = Helper.Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                }
            }
        }


        private void AddGenericFishToList(string locationName, int fishingLocation)         //From GameLocation.cs getFish()
        {
            bool magicBait = who.currentLocation.IsUsingMagicBait(who);
            if (!locationData.ContainsKey(locationName)) return;
            if (locationName.Equals("BeachNightMarket", StringComparison.Ordinal)) locationName = "Beach";

            string[] rawFishData;
            if (!magicBait) rawFishData = locationData[locationName].Split('/')[4 + Utility.getSeasonNumber(Game1.currentSeason)].Split(' '); //fish by season
            else
            {
                List<string> all_season_fish = new List<string>(); //magic bait = all fish
                for (int k = 0; k < 4; k++)
                {
                    if (locationData[locationName].Split('/')[4 + k].Split(' ').Length > 1) all_season_fish.AddRange(locationData[locationName].Split('/')[4 + k].Split(' '));
                }
                rawFishData = all_season_fish.ToArray();
            }

            Dictionary<string, string> rawFishDataWithLocation = new Dictionary<string, string>();

            if (rawFishData.Length > 1) for (int j = 0; j < rawFishData.Length; j += 2) rawFishDataWithLocation[rawFishData[j]] = rawFishData[j + 1];

            string[] keys = rawFishDataWithLocation.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                bool fail = true;
                int key = Convert.ToInt32(keys[i]);
                string[] specificFishData = fishData[key].Split('/');
                string[] timeSpans = specificFishData[5].Split(' ');
                int location = Convert.ToInt32(rawFishDataWithLocation[keys[i]]);
                if (location == -1 || fishingLocation == location)
                {
                    for (int l = 0; l < timeSpans.Length; l += 2)
                    {
                        if (Game1.timeOfDay >= Convert.ToInt32(timeSpans[l]) && Game1.timeOfDay < Convert.ToInt32(timeSpans[l + 1]))
                        {
                            fail = false;
                            break;
                        }
                    }
                }
                if (!specificFishData[7].Equals("both", StringComparison.Ordinal))
                {
                    if (specificFishData[7].Equals("rainy", StringComparison.Ordinal) && !Game1.IsRainingHere(who.currentLocation)) fail = true;
                    else if (specificFishData[7].Equals("sunny", StringComparison.Ordinal) && Game1.IsRainingHere(who.currentLocation)) fail = true;
                }
                if (magicBait) fail = false; //I guess magic bait check comes at this exact point because it overrides all conditions except rod and level?

                bool beginnersRod = who != null && who.CurrentItem != null && who.CurrentItem is FishingRod && (int)who.CurrentTool.upgradeLevel == 1;

                if (Convert.ToInt32(specificFishData[1]) >= 50 && beginnersRod) fail = true;
                if (who.FishingLevel < Convert.ToInt32(specificFishData[12])) fail = true;
                if (!fail && !fishHere.Value.Contains(key))
                {
                    if (sortMode == 0) SortItemIntoListByDisplayName(key);
                    else fishHere.Value.Add(key);

                    if (!fishChances.Value.ContainsKey(key)) fishChances.Value.Add(key, 0);
                }
            }
        }
        private void AddFishToListDynamic()                                                  //very performance intensive check for fish fish available in this area - simulates fishing
        {
            if (!(who.currentLocation is IslandSouthEast && who.getTileLocation().X >= 17 && who.getTileLocation().X <= 21 && who.getTileLocation().Y >= 19 && who.getTileLocation().Y <= 23))
            {
                int freq = (isMinigame.Value) ? 1 : extraCheckFrequency / totalPlayersOnThisPC; //minigame lowers frequency
                for (int i = 0; i < freq; i++)
                {
                    bool caughtIridiumKrobus = Game1.player.mailReceived.Contains("caughtIridiumKrobus"); //workaround for preventing player from getting something
                    int nuts = 0;
                    if (Game1.player.team.limitedNutDrops.ContainsKey("IslandFishing")) nuts = Game1.player.team.limitedNutDrops["IslandFishing"];

                    int f = who.currentLocation.getFish(0, 1, 5, who, 100, who.getTileLocation(), who.currentLocation.Name).ParentSheetIndex;

                    if ((showPercentages || sortMode == 1) && fishChances.Value[-1] < int.MaxValue) //percentages, slow version (the one shown) updated less and less with time
                    {
                        if (f >= 167 && f <= 172) fishChances.Value[168]++;
                        else if (!fishHere.Value.Contains(f))
                        {
                            if (sortMode == 0) SortItemIntoListByDisplayName(f); //sort by name
                            else fishHere.Value.Add(f);
                            fishChances.Value.Add(f, 1);
                        }
                        else fishChances.Value[f]++;
                        fishChances.Value[-1]++;
                        if (fishChances.Value[-1] % fishChancesModulo.Value == 0)
                        {
                            if (fishChancesModulo.Value < Int16.MaxValue) fishChancesModulo.Value *= 10;
                            fishChancesSlow.Value = fishChances.Value.ToDictionary(entry => entry.Key, entry => entry.Value);
                        }
                    }
                    else if ((f < 167 || f > 172) && !fishHere.Value.Contains(f))
                    {
                        if (sortMode == 0) SortItemIntoListByDisplayName(f);
                        else fishHere.Value.Add(f);
                    }

                    if (sortMode == 1) SortListByPercentages(); //sort by %

                    if (!caughtIridiumKrobus && Game1.player.mailReceived.Contains("caughtIridiumKrobus")) Game1.player.mailReceived.Remove("caughtIridiumKrobus");
                    if (Game1.player.team.limitedNutDrops.ContainsKey("IslandFishing") && Game1.player.team.limitedNutDrops["IslandFishing"] != nuts) Game1.player.team.limitedNutDrops["IslandFishing"] = nuts;
                }
            }
        }

        private void AddCrabPotFish()
        {
            fishHere.Value = new List<int>();
            bool isMariner = who.professions.Contains(10);
            if (!isMariner) fishHere.Value.Add(168);//trash
            fishChancesSlow.Value = new Dictionary<int, int>();

            bool ocean = who.currentLocation is Beach || who.currentLocation.catchOceanCrabPotFishFromThisSpot((int)who.getTileLocation().X, (int)who.getTileLocation().Y);
            float failChance = (isMariner ? 1f : 0.8f - (float)who.currentLocation.getExtraTrashChanceForCrabPot((int)who.getTileLocation().X, (int)who.getTileLocation().Y));

            foreach (var fish in fishData)
            {
                if (!fish.Value.Contains("trap")) continue;

                string[] rawSplit = fish.Value.Split('/');
                if ((rawSplit[4].Equals("ocean", StringComparison.Ordinal) && ocean) || (rawSplit[4].Equals("freshwater", StringComparison.Ordinal) && !ocean))
                {
                    if (!fishHere.Value.Contains(fish.Key))
                    {
                        if (sortMode == 0) SortItemIntoListByDisplayName(fish.Key);
                        else fishHere.Value.Add(fish.Key);

                        if (showPercentages || sortMode == 1)
                        {
                            float rawChance = float.Parse(rawSplit[2]);
                            fishChancesSlow.Value.Add(fish.Key, (int)Math.Round(rawChance * failChance * 100f));
                            failChance *= (1f - rawChance);
                        }
                    }
                }
            }
            if (showPercentages || sortMode == 1)
            {
                if (isMariner) fishChancesSlow.Value.Add(-1, fishChancesSlow.Value.Sum(x => x.Value));
                else
                {
                    fishChancesSlow.Value.Add(168, 100 - fishChancesSlow.Value.Sum(x => x.Value));
                    fishChancesSlow.Value.Add(-1, 100);
                }
                if (sortMode == 1) SortListByPercentages();
            }
        }

        private void UpdateConfig()
        {
            iconMode = config.BarIconMode;                                                                  //config: 0=Horizontal Icons, 1=Vertical Icons, 2=Vertical Icons + Text, 3=Off
            miniMode = config.MinigamePreviewMode;                                                          //config: Fish preview in minigame: 0=Full, 1=Simple, 2=BarOnly, 3=Off

            if (iconMode != 3)
            {
                barPosition = new Vector2(config.BarTopLeftLocationX + 2, config.BarTopLeftLocationY + 2);  //config: Position of bar
                barScale = config.BarScale;                                                                 //config: Custom scale for the location bar.
                maxIcons = config.BarMaxIcons;                                                              //config: ^Max amount of tackle + trash + fish icons
                maxIconsPerRow = config.BarMaxIconsPerRow;                                                  //config: ^How many per row/column.
                backgroundMode = config.BarBackgroundMode;                                                  //config: 0=Circles (dynamic), 1=Rectangle (single), 2=Off
                showTackles = config.BarShowBaitAndTackleInfo;                                              //config: Whether it should show Bait and Tackle info.
                showPercentages = config.BarShowPercentages;                                                //config: Whether it should show catch percentages.
                sortMode = config.BarSortMode;                                                              //config: 0= By Name (text mode only), 1= By Percentage, 2=Off
                extraCheckFrequency = config.BarExtraCheckFrequency;                                        //config: 20-220: Bad performance dynamic check to see if there's modded/hardcoded fish
                scanRadius = config.BarScanRadius;                                                          //config: 0: Only checks if can fish, 1-50: also checks if there's water within X tiles around player.
                uncaughtDark = config.UncaughtFishAreDark;                                                  //config: Whether uncaught fish are displayed as ??? and use dark icons
                barCrabEnabled = config.BarCrabPotEnabled;                                                  //config: If bait/tackle/bait preview is enabled when holding a fishing rod

                locationData = Game1.content.Load<Dictionary<string, string>>("Data\\Locations");   //gets location data (which fish are here)
                fishData = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");               //gets fish data

                //locationData = Helper.Content.Load<Dictionary<string, string>>("Data\\Locations", ContentSource.GameContent);
                //fishData = Helper.Content.Load<Dictionary<int, string>>("Data\\Fish", ContentSource.GameContent);

                if (backgroundMode == 0) background = WhiteCircle(17, 30);
                else background = WhitePixel();
            }
        }

        private void SortItemIntoListByDisplayName(int itemId)
        {
            string name = (new StardewValley.Object(itemId, 1).Name.Equals("Error Item", StringComparison.Ordinal)) ? new StardewValley.Objects.Furniture(itemId, Vector2.Zero).DisplayName : new StardewValley.Object(itemId, 1).DisplayName;
            for (int j = 0; j < fishHere.Value.Count; j++)
            {
                if (string.Compare(name, new StardewValley.Object(fishHere.Value[j], 1).DisplayName, StringComparison.CurrentCulture) <= 0)
                {
                    fishHere.Value.Insert(j, itemId);
                    return;
                }
            }
            fishHere.Value.Add(itemId);
        }

        private void SortListByPercentages()
        {
            int index = 0;
            foreach (var item in fishChancesSlow.Value.OrderByDescending(d => d.Value).ToList())
            {
                if (fishHere.Value.Contains(item.Key))
                {
                    fishHere.Value.Remove(item.Key);
                    fishHere.Value.Insert(index, item.Key);
                    index++;
                }
            }
        }

        private void AddBackground(SpriteBatch batch, Vector2 boxTopLeft, Vector2 boxBottomLeft, int iconCount, Rectangle source, float iconScale, float boxWidth, float boxHeight)
        {
            if (backgroundMode == 0)
            {
                batch.Draw(background, new Rectangle((int)boxBottomLeft.X - 1, (int)boxBottomLeft.Y - 1, (int)(source.Width * iconScale) + 1, (int)((source.Width * iconScale) + 1 + (showPercentages ? 10 * barScale : 0))),
                    null, new Color(0, 0, 0, 0.5f), 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
            else if (backgroundMode == 1)
            {
                if (iconMode == 0) batch.Draw(background, new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(source.Width * iconScale * Math.Min(iconCount, maxIconsPerRow)) + 5,
               (int)(((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)) * Math.Ceiling(iconCount / (maxIconsPerRow * 1.0))) + 5), null, new Color(0, 0, 0, 0.5f), 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                else if (iconMode == 1) batch.Draw(background, new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(source.Width * iconScale * Math.Ceiling(iconCount / (maxIconsPerRow * 1.0))) + 5,
                    (int)(((source.Width * iconScale) + (showPercentages ? 10 * barScale : 0)) * Math.Min(iconCount, maxIconsPerRow)) + 5), null, new Color(0, 0, 0, 0.5f), 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
                else if (iconMode == 2) batch.Draw(background, new Rectangle((int)boxTopLeft.X - 2, (int)boxTopLeft.Y - 2, (int)(boxWidth - boxTopLeft.X + 6), (int)boxHeight + 4),
                    null, new Color(0, 0, 0, 0.5f), 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
        }

        private Texture2D WhitePixel() //returns a single pixel texture that can be recoloured and resized to make up a background
        {
            Texture2D whitePixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            return whitePixel;
        }
        private Texture2D WhiteCircle(int width, int thickness) //returns a circle texture that can be recoloured and resized to make up a background. Width works better with Odd Numbers.
        {
            Texture2D whitePixel = new Texture2D(Game1.graphics.GraphicsDevice, width, width);

            Color[] data = new Color[width * width];

            float radiusSquared = (width / 2) * (width / 2);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    float dx = x - (width / 2);
                    float dy = y - (width / 2);
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared + thickness)
                    {
                        data[(x + y * width)] = Color.White;
                    }
                }
            }

            whitePixel.SetData(data);
            return whitePixel;
        }


        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == "barteke22.FishingMinigames" && e.Type == "FishCaught" && Game1.player.UniqueMultiplayerID == e.ReadAs<long>()) oldTime.Value = -1;
        }
    }
}
