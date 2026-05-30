using System;
using System.Collections.Generic;
using Fitzmark.BDRSim.Core;
using Fitzmark.BDRSim.Data;
using Fitzmark.BDRSim.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fitzmark.BDRSim.UI
{
    /// <summary>
    /// D&D-style advanced character creation: pick a Sales Style (race) with a
    /// trait and a starting ability, buy ability scores with a point budget,
    /// customize a live 3D avatar, and see how the build will play. Saves the
    /// character and returns to the menu.
    /// </summary>
    public class CharacterCreateController : MonoBehaviour
    {
        private BDRCharacter _draft;
        private int _styleIndex;

        private AvatarBuilder _avatar;
        private InputField _firstName;
        private InputField _lastName;
        private TMP_Text _pointsLabel;
        private TMP_Text _previewLabel;
        private readonly List<Action> _refreshers = new();

        private SalesStyle CurrentStyle => SalesStyleLibrary.All[_styleIndex];

        private void Start()
        {
            _draft = new BDRCharacter
            {
                styleId = SalesStyleLibrary.All[0].Id,
                attributes = PointBuy.NewBaseline(),
                avatar = new AvatarConfig()
            };
            _styleIndex = 0;

            CreateAvatar();
            BuildUi();
            RefreshAll();
        }

        // ---- avatar ---------------------------------------------------------

        private void CreateAvatar()
        {
            var go = new GameObject("Avatar");
            go.transform.position = new Vector3(-1.6f, 0f, 0f); // in the open left third of the screen
            _avatar = go.AddComponent<AvatarBuilder>();
            _avatar.SetConfig(_draft.avatar);

            // Frame the full figure straight-on in the area not covered by the panel.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetPositionAndRotation(new Vector3(0f, 0.95f, -5f), Quaternion.identity);
                cam.fieldOfView = 45f;
            }
        }

        private void ApplyAvatar() => _avatar.SetConfig(_draft.avatar);

        // ---- layout ---------------------------------------------------------

        private void BuildUi()
        {
            var canvas = UiFactory.CreateScreenCanvas("CreateCanvas");

            var panel = UiFactory.Panel(canvas.transform, UiTheme.Panel, "Panel");
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.48f, 0f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel.gameObject, pad: 16, spacing: 10, expandH: false);

            UiFactory.Label(panel.transform, "CREATE YOUR BDR", 26, UiTheme.AccentStrong,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            var content = UiFactory.MakeScrollView(panel.transform, new Color(0f, 0f, 0f, 0.12f));
            UiFactory.Size(content.parent.parent.gameObject, flexH: 1f);

            BuildNameSection(content);
            BuildStyleSection(content);
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

        private void BuildStyleSection(Transform parent)
        {
            SectionHeader(parent, "SALES STYLE");
            Stepper(parent,
                () => CurrentStyle.Name,
                () => CycleStyle(-1),
                () => CycleStyle(1));

            var desc = UiFactory.Label(parent, "", 13, UiTheme.TextMuted, TextAnchor.UpperLeft,
                FontStyle.Italic);
            _refreshers.Add(() => desc.text = CurrentStyle.Description);

            var trait = UiFactory.Label(parent, "", 13, UiTheme.AccentStrong, TextAnchor.UpperLeft,
                FontStyle.Bold);
            _refreshers.Add(() => trait.text =
                $"Trait — {CurrentStyle.TraitName}: {CurrentStyle.TraitDescription}");

            var ability = UiFactory.Label(parent, "", 13, UiTheme.Positive, TextAnchor.UpperLeft);
            _refreshers.Add(() =>
            {
                var a = AbilityLibrary.Get(CurrentStyle.GrantedAbilityId);
                ability.text = a != null ? $"Ability — {a.Name}: {a.Description}" : string.Empty;
            });
        }

        private void BuildAttributeSection(Transform parent)
        {
            SectionHeader(parent, "ABILITY SCORES (point-buy)");
            _pointsLabel = UiFactory.Label(parent, "", 14, UiTheme.Warning, TextAnchor.UpperLeft,
                FontStyle.Bold);

            foreach (var type in BDRAttributes.All)
            {
                var captured = type;
                Stepper(parent,
                    () => StatText(captured),
                    () => LowerStat(captured),
                    () => RaiseStat(captured));
            }
        }

        private void BuildAppearanceSection(Transform parent)
        {
            SectionHeader(parent, "APPEARANCE");
            Stepper(parent, () => $"Skin tone  {_draft.avatar.skinTone + 1}/{AvatarPalette.SkinCount}",
                () => Cycle(ref _draft.avatar.skinTone, -1, AvatarPalette.SkinCount),
                () => Cycle(ref _draft.avatar.skinTone, 1, AvatarPalette.SkinCount));
            Stepper(parent, () => $"Outfit  {_draft.avatar.outfitColor + 1}/{AvatarPalette.OutfitCount}",
                () => Cycle(ref _draft.avatar.outfitColor, -1, AvatarPalette.OutfitCount),
                () => Cycle(ref _draft.avatar.outfitColor, 1, AvatarPalette.OutfitCount));
            Stepper(parent, () => $"Accent  {_draft.avatar.accentColor + 1}/{AvatarPalette.AccentCount}",
                () => Cycle(ref _draft.avatar.accentColor, -1, AvatarPalette.AccentCount),
                () => Cycle(ref _draft.avatar.accentColor, 1, AvatarPalette.AccentCount));
            Stepper(parent, () => $"Hair  {_draft.avatar.hairColor + 1}/{AvatarPalette.HairCount}",
                () => Cycle(ref _draft.avatar.hairColor, -1, AvatarPalette.HairCount),
                () => Cycle(ref _draft.avatar.hairColor, 1, AvatarPalette.HairCount));
            Stepper(parent, () => $"Build:  {AvatarConfig.BuildName(_draft.avatar.build)}",
                () => Cycle(ref _draft.avatar.build, -1, 3),
                () => Cycle(ref _draft.avatar.build, 1, 3));
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

        private void CycleStyle(int delta)
        {
            _styleIndex = Wrap(_styleIndex + delta, SalesStyleLibrary.All.Count);
            _draft.styleId = CurrentStyle.Id;
        }

        private void RaiseStat(AttributeType type)
        {
            if (PointBuy.CanRaise(_draft.attributes, type))
                _draft.attributes.Adjust(type, 1);
        }

        private void LowerStat(AttributeType type)
        {
            if (PointBuy.CanLower(_draft.attributes, type))
                _draft.attributes.Adjust(type, -1);
        }

        private void Cycle(ref int field, int delta, int count) { field = Wrap(field + delta, count); ApplyAvatar(); }

        private void CycleHeight(int d)
        {
            _draft.avatar.height = Mathf.Clamp(_draft.avatar.height + d * 0.04f, 0.9f, 1.12f);
            ApplyAvatar();
        }

        private void Randomize()
        {
            _styleIndex = UnityEngine.Random.Range(0, SalesStyleLibrary.All.Count);
            _draft.styleId = CurrentStyle.Id;

            _draft.attributes = PointBuy.NewBaseline();
            int guard = 200;
            while (guard-- > 0 && PointBuy.Remaining(_draft.attributes) > 0)
            {
                var t = BDRAttributes.All[UnityEngine.Random.Range(0, BDRAttributes.All.Length)];
                if (PointBuy.CanRaise(_draft.attributes, t)) _draft.attributes.Adjust(t, 1);
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
            _draft.styleId = CurrentStyle.Id;
            _draft.attributes = FinalAttributes();
            _draft.level = 1;
            _draft.xp = 0;
            _draft.unspentSkillPoints = 0;
            _draft.unlockedPerks = new List<string>();
            _draft.career = new CareerState();

            GameManager.Instance.CreateProfile(_draft);
            GameManager.Instance.ReturnToMenu();
        }

        // ---- derived --------------------------------------------------------

        /// <summary>Point-bought scores plus the current style's modifiers (clamped).</summary>
        private BDRAttributes FinalAttributes()
        {
            var final = new BDRAttributes();
            var style = CurrentStyle;
            foreach (var t in BDRAttributes.All)
                final.Set(t, _draft.attributes.Get(t) + style.Mod(t));
            return final;
        }

        private BDRCharacter PreviewCharacter() =>
            new BDRCharacter { styleId = CurrentStyle.Id, attributes = FinalAttributes() };

        private string StatText(AttributeType t)
        {
            int pointBought = _draft.attributes.Get(t);
            int mod = CurrentStyle.Mod(t);
            int final = Mathf.Clamp(pointBought + mod, BDRAttributes.Min, BDRAttributes.Max);
            string modStr = mod == 0 ? "" : (mod > 0 ? $" +{mod}" : $" {mod}");
            return $"{BDRAttributes.DisplayName(t)}:  {final}  <size=11>[buy {pointBought}{modStr}]</size>";
        }

        private void RefreshAll()
        {
            foreach (var r in _refreshers) r();

            if (_pointsLabel != null)
                _pointsLabel.text =
                    $"Points to spend: {PointBuy.Remaining(_draft.attributes)} / {PointBuy.Budget}";

            if (_previewLabel != null)
            {
                var m = CharacterModifiers.FromCharacter(PreviewCharacter());
                _previewLabel.text =
                    $"Starting trust:  {Pct(m.TrustBonus)}\n" +
                    $"Patience drain:  x{m.PatienceDrainMultiplier:0.00}\n" +
                    $"Rate headroom:  {Pct(m.NegotiationSkill)}\n" +
                    $"Discovery edge:  {Signed(m.BonusFor(ScoreCategory.Discovery))}\n" +
                    $"Pitch / objection edge:  {Signed(m.BonusFor(ScoreCategory.ValueArticulation))}";
            }
        }

        // ---- helpers --------------------------------------------------------

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
