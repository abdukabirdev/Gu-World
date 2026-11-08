using System.Collections.Generic;
using GuWorld.Creatures;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// Who the beasts are, over their heads: each wolf near (or hunting the hero, or just struck) has its name, its
    /// level and its health over it — the beast kings' names in their rung's colour, a thunder shield turning the bar
    /// blue, a berserk king's name red. A thousand or myriad beast king that hunts the hero has its bar across the top of
    /// the screen as well.
    /// </summary>
    public partial class Menus
    {
        const int PlateCount = 16;

        readonly List<(RectTransform root, Text name, RectTransform fill, Image fillImage)> plates = new List<(RectTransform, Text, RectTransform, Image)>();
        RectTransform bossBar;
        Text bossName;
        Text bossRung;
        Text bossState;
        RectTransform bossFill;
        Image bossFillImage;
        Camera plateCamera;

        static readonly Color[] TierColours =
        {
            UiKit.Hex(0xEAF0F2),
            UiKit.Hex(0xF0C868),
            UiKit.Hex(0xF0A070),
            UiKit.Hex(0xF08A74),
        };

        void BuildPlates()
        {
            var layer = UiKit.Rect("Plates", hud).Fill();
            layer.SetAsFirstSibling();

            for (var i = 0; i < PlateCount; i++)
            {
                var root = UiKit.Rect("Plate", layer);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(170, 34);
                var name = UiKit.Label("", root, 14, UiKit.Text, TextAnchor.LowerCenter).Shadowed();
                name.rectTransform.Place(Vector2.zero, Vector2.one, -40, 9, -40, 0);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                var back = UiKit.Image("Bar", root, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Plain);
                back.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 31, 0, 31, -5);
                UiKit.Frame(back.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f));
                var fill = UiKit.Image("Fill", back.transform, UiKit.Danger, UiKit.Plain);
                fill.rectTransform.Place(Vector2.zero, new Vector2(0, 1));
                root.gameObject.SetActive(false);
                plates.Add((root, name, fill.rectTransform, fill));
            }

            // A king that hunts the hero: its rung in spaced capitals, its name, a long bar across the top.
            bossBar = UiKit.Rect("Boss", hudStage);
            bossBar.anchorMin = bossBar.anchorMax = new Vector2(0.5f, 1);
            bossBar.pivot = new Vector2(0.5f, 1);
            bossBar.anchoredPosition = new Vector2(0, -22);
            bossBar.sizeDelta = new Vector2(560, 64);
            bossRung = UiKit.Lbl("", bossBar, 11, UiKit.Danger, 0.34f, TextAnchor.MiddleCenter).Shadowed();
            bossRung.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -14, 0, 0);
            bossName = UiKit.Disp("", bossBar, 26, UiKit.Text, TextAnchor.MiddleCenter).Shadowed();
            bossName.rectTransform.Place(new Vector2(0, 1), Vector2.one, -100, -46, -100, 18);
            var bossBack = UiKit.Image("Bar", bossBar, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Plain);
            bossBack.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 4, 0, -12);
            UiKit.Frame(bossBack.transform, UiKit.Danger.A(0.35f));
            bossFillImage = UiKit.Image("Fill", bossBack.transform, Color.white, UiKit.Plain);
            bossFillImage.Gradient(UiKit.Hex(0x5A1A12), UiKit.Danger, true);
            UiKit.Glow(bossFillImage.transform, UiKit.Danger.A(0.3f), 12);
            bossFill = bossFillImage.rectTransform;
            bossFill.Place(Vector2.zero, new Vector2(0, 1));
            bossState = UiKit.Label("", bossBar, 10, UiKit.Text2, TextAnchor.UpperCenter).Shadowed();
            bossState.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, -14, 0, 2);
            bossBar.gameObject.SetActive(false);
        }

        void UpdatePlates()
        {
            if (plates.Count == 0)
            {
                return;
            }

            if (plateCamera == null && host.View != null)
            {
                plateCamera = host.View.GetComponent<Camera>();
            }

            var used = 0;
            IFoe boss = null;
            var bossDistance = float.PositiveInfinity;
            var playing = Current == Page.None;

            if (plateCamera != null && playing)
            {
                var eye = plateCamera.transform.position;
                var canvasRect = (RectTransform)hud;

                foreach (var wolf in Foes.All)
                {
                    if (!(wolf is Object alive) || alive == null || wolf.Dead || used >= plates.Count)
                    {
                        continue;
                    }

                    var top = wolf.transform.position + Vector3.up * (wolf.Height + 0.45f);
                    var distance = Vector3.Distance(eye, top);
                    var near = distance < 30 + 15 * wolf.Tier || wolf.Hunting || Time.time - wolf.StruckAt < 8;

                    if (wolf.Tier >= 2 && wolf.Hunting && distance < 90 && distance < bossDistance)
                    {
                        boss = wolf;
                        bossDistance = distance;
                    }

                    if (!near || distance > 70)
                    {
                        continue;
                    }

                    var screen = plateCamera.WorldToScreenPoint(top);

                    if (screen.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local))
                    {
                        continue;
                    }

                    var (root, name, fill, fillImage) = plates[used++];
                    root.gameObject.SetActive(true);
                    root.anchoredPosition = local + new Vector2(0, 10);
                    // Smaller far off, never too small to read.
                    root.localScale = Vector3.one * Mathf.Clamp(16 / Mathf.Max(1, distance), 0.95f, 1.2f);
                    var colour = wolf.Tamed ? UiKit.EssenceLit : wolf.Berserk ? UiKit.Danger : TierColours[Mathf.Clamp(wolf.Tier, 0, 3)];
                    name.text = $"{wolf.Title} <size=11><color=#7E9093>{(wolf is GuMaster ? wolf.Rung : $"ур. {wolf.Level}")}</color></size>" + (wolf.Tamed ? " <size=11><color=#A8F0DC>· ваш</color></size>" : "");
                    name.color = colour;
                    fill.anchorMax = new Vector2(Mathf.Clamp01(wolf.Health / wolf.MaxHealth), 1);
                    fillImage.color = wolf.Shielded ? UiKit.Moon : wolf.Tamed ? UiKit.Essence : UiKit.Danger;
                }
            }

            for (var i = used; i < plates.Count; i++)
            {
                if (plates[i].root.gameObject.activeSelf)
                {
                    plates[i].root.gameObject.SetActive(false);
                }
            }

            bossBar.gameObject.SetActive(boss != null && playing);

            if (boss != null)
            {
                bossName.text = boss.Title;
                bossRung.text = (boss.Rung + (boss is GuMaster ? "" : $" · уровень {boss.Level}")).ToUpperInvariant();
                bossRung.color = TierColours[Mathf.Clamp(boss.Tier, 0, 3)];
                bossFill.anchorMax = new Vector2(Mathf.Clamp01(boss.Health / boss.MaxHealth), 1);
                bossFillImage.color = boss.Shielded ? UiKit.Moon : boss.Berserk ? UiKit.Hex(0xFF6040) : Color.white;
                bossState.text = $"{Mathf.CeilToInt(boss.Health)} / {Mathf.CeilToInt(boss.MaxHealth)}" + boss.State;
            }
        }
    }
}
