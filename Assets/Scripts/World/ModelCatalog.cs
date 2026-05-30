using System.Collections.Generic;

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Maps friendly model keys (e.g. "desk", "office/couch") to real imported model
    /// paths under Resources, with a per-kit scale so they come in at correct real-world
    /// size. The Kenney FBX kits import very large (a desk measures ~3.84 units tall, per
    /// its model report), so a single uniform kit scale brings the whole set to human
    /// scale while keeping every piece consistent with the others.
    /// </summary>
    public static class ModelCatalog
    {
        /// <summary>Kenney furniture-kit FBX import ~5x too big; this brings it to metres.</summary>
        public const float FurnitureKitScale = 0.2f;

        private const string Furn = "Models/kenney_furniture-kit/Models/FBX format/";

        public struct Entry
        {
            public string Path;
            public float Scale;
            public Entry(string path, float scale) { Path = path; Scale = scale; }
        }

        private static readonly Dictionary<string, Entry> Map = new();

        static ModelCatalog()
        {
            // --- office prop keys used by OfficeController ---
            F("office/reception", "kitchenBar");
            F("office/desk", "desk");
            F("office/exec_desk", "deskCorner");
            F("office/outreach_desk", "desk");
            F("office/computer", "computerScreen");
            F("office/board", "cabinetTelevision");
            F("office/rankings_board", "cabinetTelevision");
            F("office/trophycase", "bookcaseClosedWide");
            F("office/whiteboard", "paneling");
            F("office/shelf", "bookcaseOpen");
            F("office/server", "bookcaseClosed");
            F("office/plant", "pottedPlant");
            F("office/couch", "loungeSofa");
            F("office/watercooler", "kitchenFridgeSmall");

            // --- semantic keys (workstations, decor) ---
            F("desk", "desk");
            F("deskCorner", "deskCorner");
            F("chair", "chairDesk");
            F("computer", "computerScreen");
            F("keyboard", "computerKeyboard");
            F("laptop", "laptop");
            F("plant", "pottedPlant");
            F("plantSmall", "plantSmall1");
            F("sofa", "loungeSofa");
            F("sofaLong", "loungeSofaLong");
            F("table", "table");
            F("coffee", "kitchenCoffeeMachine");
            F("trashcan", "trashcan");
            F("ceilingLamp", "lampSquareCeiling");
            F("floorLamp", "lampSquareFloor");

            // --- room construction (furniture kit has these too) ---
            F("wall", "wall");
            F("wallCorner", "wallCorner");
            F("wallDoorway", "wallDoorway");
            F("wallDoorwayWide", "wallDoorwayWide");
            F("wallWindow", "wallWindow");
            F("floorTile", "floorFull");
        }

        private static void F(string key, string furnitureModel) =>
            Map[key] = new Entry(Furn + furnitureModel, FurnitureKitScale);

        public static bool TryResolve(string key, out Entry entry) => Map.TryGetValue(key, out entry);
    }
}
