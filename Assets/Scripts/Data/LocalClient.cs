using System.Collections.Generic;

namespace Fitzmark.BDRSim.Data
{
    /// <summary>
    /// A LOCAL account (Texas). You travel to these in person (via a platformer
    /// level) and advance them through meeting stages over in-game time. The wider
    /// national (USA) book is grown remotely (calls/email/video) — not here.
    /// MapX/MapZ are rough geographic positions on the Texas overworld.
    /// </summary>
    public class LocalClient
    {
        public readonly string Id;
        public readonly string Company;
        public readonly string City;
        public readonly float MapX;
        public readonly float MapZ;
        public readonly int RequiredLevel;

        public LocalClient(string id, string company, string city, float mapX, float mapZ, int requiredLevel)
        {
            Id = id;
            Company = company;
            City = city;
            MapX = mapX;
            MapZ = mapZ;
            RequiredLevel = requiredLevel;
        }
    }

    public static class TerritoryRegistry
    {
        // Positions are rough: x = west→east, z = south→north (reads as Texas).
        public static readonly List<LocalClient> All = new()
        {
            new LocalClient("austin", "Hill Country Materials", "Austin, TX", 4f, -6f, 1),
            new LocalClient("san_antonio", "Alamo Distribution", "San Antonio, TX", 0f, -16f, 1),
            new LocalClient("waco", "Brazos Manufacturing", "Waco, TX", 6f, 4f, 1),
            new LocalClient("fort_worth", "Stockyard Supply Co.", "Fort Worth, TX", 2f, 14f, 2),
            new LocalClient("dallas", "Trinity Freight Foods", "Dallas, TX", 9f, 14f, 2),
            new LocalClient("corpus", "Gulf Coast Mills", "Corpus Christi, TX", 14f, -26f, 2),
            new LocalClient("houston", "Bayou City Components", "Houston, TX", 22f, -10f, 3),
            new LocalClient("lubbock", "Plains Industrial", "Lubbock, TX", -16f, 18f, 3),
            new LocalClient("amarillo", "Panhandle Products", "Amarillo, TX", -12f, 30f, 4),
            new LocalClient("el_paso", "Sun City Logistics Group", "El Paso, TX", -34f, 6f, 5),
        };

        public static LocalClient Get(string id)
        {
            foreach (var c in All)
                if (c.Id == id) return c;
            return null;
        }
    }
}
