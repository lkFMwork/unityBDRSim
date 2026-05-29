using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// "Create your BDR" screen: a live 3D avatar on the left, and a panel on the
    /// right to set name, background (archetype), attributes (point-buy), and look.
    /// A preview shows how the build will change calls. Saves the character and
    /// returns to the menu. Attach to a GameObject in the CharacterCreate scene.
    /// </summary>
    public class CharacterCreateController : MonoBehaviour
    {
        private BDRCharacter _draft;
        private int _archetypeIndex;
        private int _pointsLeft;

        private AvatarBuilder _avatar;
        private InputField _firstName;
        private InputField _lastName;
        private Text _pointsLabel;
        private Text _previewLabel;
        private readonly List<Action> _refreshers = new();

        private void Start()
        {
            _draft = new BDRCharacter { avatar = new AvatarConfig() };
            ApplyArchetype(0);

            CreateAvatar();
            BuildUi();
            RefreshAll();
        }

        // ---- avatar ---------------------------------------------------------

        private void CreateAvatar()
        {
            var go = new GameObject("Avatar");
            go.transform.position = new Vector3(-0.8f, 0f, 0f);
            _avatar = go.AddComponent<AvatarBuilder>();
            _avatar.SetConfig(_draft.avatar);
        }

        private void ApplyAvatar() => _avatar.SetConfig(_draft.avatar);

        // ---- layout ---------------------------------------------------------

        private void BuildUi()
        {
            var canvas = UiFactory.CreateScreenCanvas("CreateCanvas");

            // Right-hand panel; the left ~half of the screen shows the 3D avatar.
            var panel = UiFactory.Panel(canvas.transform, UiTheme.Panel, "Panel");
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel.gameObject, pad: 16, spacing: 10, expandH: false);

            UiFactory.Label(panel.transform, "CREATE YOUR BDR", 26, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(panel.transform, new Color(0f, 0f, 0f, 0.12f));
            var scrollRoot = content.parent.parent.gameObject; // content -> viewport -> scroll root
            UiFactory.Size(scrollRoot, flexH: 1f);

            BuildNameSection(content);
            BuildArchetypeSection(content);
            BuildAttributeSection(content);
            BuildAppearanceSection(content);
            BuildPreviewSection(content);

            BuildFooter(panel.transform);
        }

        private void BuildNameSection(Transform parent)
        {
            SectionHeader(parent, "NAME");
            var row = UiFactory.Panel(parent, Clear, "NameRow").gameObject;
            UiFactory.HLayout(row, spacing: 8, expandW: true, expandH: true);
            UiFactory.Size(row, prefH: 38f, flexW: 1f);

            _firstName = UiFactory.InputField(row.transform, "First name", _draft.firstName);
            UiFactory.Size(_firstName.gameObject, flexW: 1f, prefH: 36f);
            _lastName = UiFactory.InputField(row.transform, "Last name", _draft.lastName);
            UiFactory.Size(_lastName.gameObject, flexW: 1f, prefH: 36f);
        }

        private void BuildArchetypeSection(Transform parent)
        {
            SectionHeader(parent, "BACKGROUND");
            Stepper(parent,
                () => ArchetypeLibrary.All[_archetypeIndex].Name,
                () => ApplyArchetype(_archetypeIndex - 1),
                () => ApplyArchetype(_archetypeIndex + 1));

            var desc = UiFactory.Label(parent, "", 13, UiTheme.TextMuted, TextAnchor.UpperLeft,
                FontStyle.Italic);
            _refreshers.Add(() => desc.text = ArchetypeLibrary.All[_archetypeIndex].Description);
        }

        private void BuildAttributeSection(Transform parent)
        {
            SectionHeader(parent, "ATTRIBUTES");
            _pointsLabel = UiFactory.Label(parent, "", 14, UiTheme.Warning, TextAnchor.UpperLeft,
                FontStyle.Bold);

            foreach (var type in BDRAttributes.All)
            {
                var captured = type;
                Stepper(parent,
                    () => $"{BDRAttributes.DisplayName(captured)}:  {_draft.attributes.Get(captured)}",
                    () => DecAttr(captured),
                    () => IncAttr(captured));
            }
        }

        private void BuildAppearanceSection(Transform parent)
        {
            SectionHeader(parent, "APPEARANCE");

            Stepper(parent, () => $"Skin tone  {_draft.avatar.skinTone + 1}/{AvatarPalette.SkinCount}",
                () => CycleSkin(-1), () => CycleSkin(1));
            Stepper(parent, () => $"Outfit  {_draft.avatar.outfitColor + 1}/{AvatarPalette.OutfitCount}",
                () => CycleOutfit(-1), () => CycleOutfit(1));
            Stepper(parent, () => $"Accent  {_draft.avatar.accentColor + 1}/{AvatarPalette.AccentCount}",
                () => CycleAccent(-1), () => CycleAccent(1));
            Stepper(parent, () => $"Hair  {_draft.avatar.hairColor + 1}/{AvatarPalette.HairCount}",
                () => CycleHair(-1), () => CycleHair(1));
            Stepper(parent, () => $"Build:  {AvatarConfig.BuildName(_draft.avatar.build)}",
                () => CycleBuild(-1), () => CycleBuild(1));
            Stepper(parent, () => $"Height:  {_draft.avatar.height:0.00}x",
                () => CycleHeight(-1), () => CycleHeight(1));
        }

        private void BuildPreviewSection(Transform parent)
        {
            SectionHeader(parent, "ON THE CALL");
            _previewLabel = UiFactory.Label(parent, "", 14, UiTheme.TextPrimary, TextAnchor.UpperLeft);
        }

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.Panel(parent, Clear, "Footer").gameObject;
            UiFactory.HLayout(footer, spacing: 10, expandW: true, expandH: true);
            UiFactory.Size(footer, prefH: 56f, flexH: 0f);

            var rand = UiFactory.Button(footer.transform, "Randomize", Randomize,
                UiTheme.PanelDark, UiTheme.TextPrimary, 16, TextAnchor.MiddleCenter);
            UiFactory.Size(rand.gameObject, prefW: 150f);

            var start = UiFactory.Button(footer.transform, "Start Career ▶", StartCareer,
                UiTheme.Positive, Color.white, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(start.gameObject, flexW: 1f);
        }

        // ---- mutations ------------------------------------------------------

        private void ApplyArchetype(int index)
        {
            int count = ArchetypeLibrary.All.Count;
            _archetypeIndex = ((index % count) + count) % count;
            var archetype = ArchetypeLibrary.All[_archetypeIndex];
            _draft.archetypeId = archetype.Id;
            _draft.attributes = archetype.BaseAttributes.Clone();
            _pointsLeft = ArchetypeLibrary.StartingBonusPoints;
        }

        private void IncAttr(AttributeType type)
        {
            if (_pointsLeft <= 0 || _draft.attributes.Get(type) >= BDRAttributes.Max) return;
            _draft.attributes.Adjust(type, 1);
            _pointsLeft--;
        }

        private void DecAttr(AttributeType type)
        {
            int baseValue = ArchetypeLibrary.All[_archetypeIndex].BaseAttributes.Get(type);
            if (_draft.attributes.Get(type) <= baseValue) return;
            _draft.attributes.Adjust(type, -1);
            _pointsLeft++;
        }

        private void CycleSkin(int d) { _draft.avatar.skinTone = Wrap(_draft.avatar.skinTone + d, AvatarPalette.SkinCount); ApplyAvatar(); }
        private void CycleOutfit(int d) { _draft.avatar.outfitColor = Wrap(_draft.avatar.outfitColor + d, AvatarPalette.OutfitCount); ApplyAvatar(); }
        private void CycleAccent(int d) { _draft.avatar.accentColor = Wrap(_draft.avatar.accentColor + d, AvatarPalette.AccentCount); ApplyAvatar(); }
        private void CycleHair(int d) { _draft.avatar.hairColor = Wrap(_draft.avatar.hairColor + d, AvatarPalette.HairCount); ApplyAvatar(); }
        private void CycleBuild(int d) { _draft.avatar.build = Wrap(_draft.avatar.build + d, 3); ApplyAvatar(); }
        private void CycleHeight(int d)
        {
            _draft.avatar.height = Mathf.Clamp(_draft.avatar.height + d * 0.04f, 0.9f, 1.12f);
            ApplyAvatar();
        }

        private void Randomize()
        {
            ApplyArchetype(UnityEngine.Random.Range(0, ArchetypeLibrary.All.Count));
            int guard = 64;
            while (_pointsLeft > 0 && guard-- > 0)
            {
                var t = BDRAttributes.All[UnityEngine.Random.Range(0, BDRAttributes.All.Length)];
                IncAttr(t);
            }
            _draft.avatar.skinTone = UnityEngine.Random.Range(0, AvatarPalette.SkinCount);
            _draft.avatar.outfitColor = UnityEngine.Random.Range(0, AvatarPalette.OutfitCount);
            _draft.avatar.accentColor = UnityEngine.Random.Range(0, AvatarPalette.AccentCount);
            _draft.avatar.hairColor = UnityEngine.Random.Range(0, AvatarPalette.HairCount);
            _draft.avatar.build = UnityEngine.Random.Range(0, 3);
            _draft.avatar.height = 0.9f + UnityEngine.Random.Range(0, 6) * 0.04f;
            ApplyAvatar();
            RefreshAll();
        }

        private void StartCareer()
        {
            _draft.firstName = Sanitize(_firstName != null ? _firstName.text : null, "New");
            _draft.lastName = Sanitize(_lastName != null ? _lastName.text : null, "Rep");
            _draft.level = 1;
            _draft.xp = 0;
            _draft.unspentSkillPoints = 0;

            GameManager.Instance.CreateProfile(_draft);
            GameManager.Instance.ReturnToMenu();
        }

        // ---- helpers --------------------------------------------------------

        private void RefreshAll()
        {
            foreach (var r in _refreshers) r();

            if (_pointsLabel != null)
                _pointsLabel.text = $"Points to spend: {_pointsLeft}";

            if (_previewLabel != null)
            {
                var m = CharacterModifiers.FromAttributes(_draft.attributes);
                _previewLabel.text =
                    $"Starting trust:  {Pct(m.TrustBonus)}\n" +
                    $"Patience drain:  x{m.PatienceDrainMultiplier:0.00}\n" +
                    $"Rate headroom:  {Pct(m.NegotiationSkill)}\n" +
                    $"Discovery edge:  {Signed(m.BonusFor(ScoreCategory.Discovery))}\n" +
                    $"Pitch / objection edge:  {Signed(m.BonusFor(ScoreCategory.ValueArticulation))}";
            }
        }

        private void SectionHeader(Transform parent, string text) =>
            UiFactory.Label(parent, text, 13, UiTheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold);

        private void Stepper(Transform parent, Func<string> text, Action prev, Action next)
        {
            var row = UiFactory.Panel(parent, Clear, "Stepper").gameObject;
            UiFactory.HLayout(row, spacing: 6, expandW: false, expandH: true);
            UiFactory.Size(row, prefH: 40f, flexW: 1f);

            var left = UiFactory.Button(row.transform, "◄", () => { prev(); RefreshAll(); },
                UiTheme.PanelDark, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(left.gameObject, prefW: 44f);

            var value = UiFactory.Label(row.transform, text(), 15, UiTheme.TextPrimary,
                TextAnchor.MiddleCenter);
            UiFactory.Size(value.gameObject, flexW: 1f);

            var right = UiFactory.Button(row.transform, "►", () => { next(); RefreshAll(); },
                UiTheme.PanelDark, UiTheme.TextPrimary, 18, TextAnchor.MiddleCenter);
            UiFactory.Size(right.gameObject, prefW: 44f);

            _refreshers.Add(() => value.text = text());
        }

        private static int Wrap(int v, int count) => ((v % count) + count) % count;

        private static string Sanitize(string s, string fallback) =>
            string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();

        private static string Pct(float v) => (v >= 0f ? "+" : "") + Mathf.RoundToInt(v * 100f) + "%";

        private static string Signed(float v) => (v >= 0f ? "+" : "") + v.ToString("0.0");

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    }
}
