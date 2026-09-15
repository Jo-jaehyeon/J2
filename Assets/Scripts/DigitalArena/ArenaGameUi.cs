using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalArena
{
    // Retained elements render at the panel's actual pixel density. Logical positions
    // stay shared with world picking and safe-area layout.
    public sealed class ArenaGameUi : IDisposable
    {

        readonly GameObject				owner;
        readonly PanelSettings			settings;
        readonly VisualElement			screen;
        readonly List<VisualElement>	boxes = new List<VisualElement>();
        readonly List<Label>			labels = new List<Label>();

        readonly List<Image>	images = new List<Image>();

        readonly Dictionary<string, Control>	buttons = new Dictionary<string, Control>();
        int										boxCount, labelCount, imageCount, order;
        public bool Enabled { get; set; } = true;

        sealed class Control
        {

            public Action	Action;

            public bool	Used;

            public Button	Element;
        }

        public ArenaGameUi(Transform parent, Font font)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Arena UI Panel Settings";
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Arena/ArenaTheme");
            owner = new GameObject("Arena UI (UI Toolkit)");
            owner.transform.SetParent(parent, false);

            var document = owner.AddComponent<UIDocument>();

            document.panelSettings = settings;
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Arena/ArenaView");
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            document.rootVisualElement.style.unityFontDefinition = FontDefinition.FromFont(font);
            screen = document.rootVisualElement.Q("arenaScreen");
            screen.pickingMode = PickingMode.Ignore;
        }

        public void Begin(float scale, Vector2 offset)
        {
            if (!Mathf.Approximately(settings.scale, scale))
            {
                settings.scale = scale;
            }

            screen.style.left = offset.x / scale;
            screen.style.top = offset.y / scale;
            boxCount = labelCount = imageCount = order = 0;
            Enabled = true;

            foreach (var control in buttons.Values)
            {
                control.Used = false;
            }
        }

        T Take<T>(List<T> pool, ref int count)
            where T : VisualElement, new()
        {
            if (count == pool.Count)
            {
                var element = new T
                {
                    pickingMode = PickingMode.Ignore
                };

                element.AddToClassList("arena-element");
                pool.Add(element);
            }

            return pool[count++];
        }

        void Place(VisualElement element, Rect rect)
        {
            if (element.parent != screen)
            {
                screen.Insert(order, element);
            }
            else if (screen.IndexOf(element) != order)
            {
                element.RemoveFromHierarchy();
                screen.Insert(order, element);
            }

            order++;
            element.style.display = DisplayStyle.Flex;
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = Mathf.Max(0, rect.width);
            element.style.height = Mathf.Max(0, rect.height);
        }

        public void Fill(Rect rect, Color color)
        {
            var element = Take(boxes, ref boxCount);

            element.style.backgroundColor = color;
            Place(element, rect);
        }

        public void Text(Rect rect, string value, int size, Color color, bool bold, TextAnchor alignment)
        {
            var element = Take(labels, ref labelCount);

            element.enableRichText = false;
            element.text = value;
            element.style.fontSize = size;
            element.style.color = color;
            element.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            element.style.unityTextAlign = alignment;
            Place(element, rect);
        }

        public void Picture(Rect rect, Texture texture, ScaleMode mode = ScaleMode.StretchToFill, float opacity = 1)
        {
            var element = Take(images, ref imageCount);

            element.image = texture;
            element.scaleMode = mode;
            element.style.opacity = opacity;
            Place(element, rect);
        }

        public void Button(string id, Rect rect, string value, Color color, Action action, bool enabled = true, bool transparent = false, string tooltip = null)
        {
            if (!buttons.TryGetValue(id, out var control))
            {
                control = new Control
                {
                    Element = new Button
                    {
                        name = id
                    }
                };
                control.Element.AddToClassList("arena-element");
                control.Element.AddToClassList("arena-button");

                var captured = control;

                control.Element.clicked += () =>
                {
                    if (captured.Used && captured.Element.enabledInHierarchy)
                    {
                        captured.Action?.Invoke();
                    }
                };
                buttons.Add(id, control);
            }

            control.Used = true;
            control.Action = action;
            control.Element.text = value;
            control.Element.tooltip = tooltip;
            control.Element.EnableInClassList("arena-hit-target", transparent);
            control.Element.style.backgroundColor = color;
            control.Element.SetEnabled(Enabled && enabled);
            Place(control.Element, rect);
        }

        public void End()
        {
            HideUnused(boxes, boxCount);
            HideUnused(labels, labelCount);
            HideUnused(images, imageCount);

            foreach (var control in buttons.Values)
            {
                if (!control.Used)
                {
                    control.Element.style.display = DisplayStyle.None;
                    control.Action = null;
                }
            }
        }

        static void HideUnused<T>(List<T> pool, int count)
            where T : VisualElement
        {
            for (int i = count; i < pool.Count; i++)
            {
                pool[i].style.display = DisplayStyle.None;
            }
        }

        public void Dispose()
        {
            if (owner != null)
            {
                UnityEngine.Object.Destroy(owner);
            }

            if (settings != null)
            {
                UnityEngine.Object.Destroy(settings);
            }
        }
    }
}
