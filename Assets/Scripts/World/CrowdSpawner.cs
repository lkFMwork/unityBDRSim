using Fitzmark.BDRSim.Data;
using UnityEngine;
using AvatarBuilder = Fitzmark.BDRSim.UI.AvatarBuilder; // disambiguate from UnityEngine.AvatarBuilder

namespace Fitzmark.BDRSim.World
{
    /// <summary>
    /// Populates a hub scene with ambient rigged pedestrians — randomly styled BDRs who
    /// wander and stroll — so the office/city feel inhabited. Spawns at runtime; drop one
    /// in a scene and set the center/radius/count.
    /// </summary>
    public class CrowdSpawner : MonoBehaviour
    {
        public int count = 6;
        public float radius = 12f;
        public Vector3 center = Vector3.zero;
        public float groundY = 0f;

        private void Start()
        {
            for (int i = 0; i < count; i++) Spawn(i);
        }

        private void Spawn(int i)
        {
            var go = new GameObject("NPC_" + i);
            Vector2 p = Random.insideUnitCircle * radius;
            go.transform.position = center + new Vector3(p.x, groundY, p.y);

            // Real Mixamo body when imported; else the procedural avatar.
            if (OfficeWorker.Attach(go.transform) == null)
            {
                var body = new GameObject("Body");
                body.transform.SetParent(go.transform, false);
                body.AddComponent<AvatarBuilder>().SetConfig(RandomConfig());
            }

            var npc = go.AddComponent<WanderingNpc>();
            npc.center = center;
            npc.radius = radius;
            npc.speed = Random.Range(1.7f, 3.0f);
        }

        private static AvatarConfig RandomConfig() => new AvatarConfig
        {
            skinTone = Random.Range(0, AvatarPalette.SkinCount),
            outfitColor = Random.Range(0, AvatarPalette.OutfitCount),
            accentColor = Random.Range(0, AvatarPalette.AccentCount),
            hairColor = Random.Range(0, AvatarPalette.HairCount),
            build = Random.Range(0, 3),
            height = Random.Range(0.92f, 1.08f)
        };
    }
}
