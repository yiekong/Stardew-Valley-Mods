﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using PyTK.Extensions;
using PyTK.CustomElementHandler;
using System.Linq;
using StardewModdingAPI.Events;
using PyTK.Types;
using PyTK.Tiled;
using System.IO;
using xTile;
using System;
using Microsoft.Xna.Framework.Input;

namespace JoJaBan
{
    public class JoJaBanMod : Mod
    {
        internal static CustomObjectData boxData;
        internal static CustomObjectData arcadeData;
        internal static IModHelper SHelper;
        internal static Texture2D boxTexture;
        internal static Texture2D arcadeTexture;
        internal static int highestLevel = 1;
        internal static int maxLevel = 1;
        internal static int currentLevel = 1;
        internal static GameLocation lastLocation = null;
        internal static Vector2 lastPosition = Vector2.Zero;

        public override void Entry(IModHelper helper)
        {
            SHelper = helper;
            maxLevel = Directory.GetFiles(Path.Combine(Helper.DirectoryPath, "Level"), "*.tmx", SearchOption.AllDirectories).Count();

            TileAction exitAction = new TileAction("JoJaBan.Exit", exitGame);
            exitAction.register();
            TileAction startAction = new TileAction("JoJaBan.Start", startGame);
            startAction.register();

            arcadeTexture = helper.Content.Load<Texture2D>(@"Assets/arcade.png");
            arcadeData = new CustomObjectData("JoJaBan", "JoJaBan/0/-300/Crafting -9/Play 'JoJaBan by Platonymous' at home!/true/true/0/JoJaBan", arcadeTexture, Color.White, bigCraftable: true, type: typeof(JoJaBanMachine));
            Texture2D townInterior = Helper.Content.Load<Texture2D>(@"Maps/townInterior", ContentSource.GameContent);
            boxTexture = townInterior.getArea(new Rectangle(304, 1024, 16, 32));
            boxData = new CustomObjectData("JoJa Box", "JoJa Box/0/-300/Crafting -9/JoJa Box/true/true/0/JoJa Box", boxTexture, Color.White, bigCraftable: true, type:typeof(JoJaBox));
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;
            SaveEvents.AfterLoad += (o, e) => addToCatalogue();
        }

        public static void addToCatalogue()
        {
            new InventoryItem(arcadeData.getObject(), 5000, 1).addToNPCShop("Gus");
        }

        private static void selectLevel(int number, int price, Farmer who)
        {
            lastLocation = Game1.currentLocation;
            lastPosition = Game1.player.getTileLocation();

            currentLevel = number;
            GameEvents.UpdateTick += startNextLevel;
        }

        private static bool exitGame(string s, GameLocation gl, Vector2 vec, string st)
        {
            Game1.warpFarmer(lastLocation.name.Value, (int)lastPosition.X, (int)lastPosition.Y, 0);
            Game1.displayHUD = true;
            return true;
        }

        internal static bool startGame(string s, GameLocation gl, Vector2 vec, string st)
        {
            if (highestLevel == 1)
            {
                lastLocation = Game1.currentLocation;
                lastPosition = Game1.player.getTileLocation();
                currentLevel = highestLevel;
                GameEvents.UpdateTick += startNextLevel;
            }
            else
                Game1.activeClickableMenu = new NumberSelectionMenu("JoJaBan : Select Level", selectLevel, -1, 1, highestLevel, highestLevel);

            return true;
        }

        private static void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            if (e.KeyPressed == Keys.Escape && Game1.currentLocation.Name.StartsWith("JoJaBanLevel"))
            {
                resetLevel(Game1.currentLocation);
                loadLevel(currentLevel);
            }
        }

        private static void loadLevel(int l)
        {
            Game1.player.canOnlyWalk = true;
            if (l > maxLevel)
                l = 1;

            highestLevel = Math.Max(l, highestLevel);
            currentLevel = l;

            var check = Game1.getLocationFromName($"JoJaBanLevel{l}");

            if (check is GameLocation gl && gl.name.Value == $"JoJaBanLevel{l}")
            {
                string[] startTile = ((string)gl.map.Properties["Start"]).Split(',');
                Vector2 startTilePos = new Vector2(int.Parse(startTile[0]), int.Parse(startTile[1]));
                Game1.warpFarmer($"JoJaBanLevel{l}", (int)startTilePos.X, (int)startTilePos.Y, 0);
                resetLevel(Game1.getLocationFromName($"JoJaBanLevel{l}"));
                return;
            }

            Map level = TMXContent.Load(Path.Combine("Level", $"level{l}.tmx"), SHelper);
            string[] start = ((string)level.Properties["Start"]).Split(',');
            Vector2 startPos = new Vector2(int.Parse(start[0]), int.Parse(start[1]));

            string[] exit = ((string)level.Properties["Exit"]).Split(',');
            Vector2 exitPos = new Vector2(int.Parse(exit[0]), int.Parse(exit[1]));
            level.GetLayer("Buildings").Tiles[(int)exitPos.X, (int)exitPos.Y].Properties.Add("Action", "JoJaBan.Exit");

            level.inject($"Maps/JoJaBanLevel{l}");

            GameLocation levelLocation = new GameLocation(Path.Combine("Maps", $"JoJaBanLevel{l}"), $"JoJaBanLevel{l}");
            Game1.warpFarmer($"JoJaBanLevel{l}", (int)startPos.X, (int)startPos.Y, 0);
            resetLevel(Game1.getLocationFromName($"JoJaBanLevel{l}"));
        }

        internal static bool nextLevel(GameLocation level)
        {
            currentLevel++;
            GameEvents.UpdateTick += startNextLevel;
            return true;
        }

        private static void startNextLevel(object sender, EventArgs e)
        {
            Game1.displayHUD = false;
            loadLevel(currentLevel);
            GameEvents.UpdateTick -= startNextLevel;
        }

        private static bool resetLevel(GameLocation level)
        {
            level.objects.Clear();
            var layer = level.map.GetLayer("Boxes");

            for (int x = 0; x < layer.LayerWidth; ++x)
                for (int y = 0; y < layer.LayerHeight; ++y)
                    if (layer.Tiles[x, y] != null)
                    {
                        var pos = new Vector2(x, y);
                        var newBox = new JoJaBox(boxData, pos);
                        newBox.onTarget = level.map.GetLayer("Back").Tiles[x, y].TileIndex.ToString() == level.map.Properties["Target"];
                        level.objects.Add(pos,newBox);
                    }

            Game1.drawDialogueNoTyping("Level " + JoJaBanMod.currentLevel);

            return true;
        }
    }
}
