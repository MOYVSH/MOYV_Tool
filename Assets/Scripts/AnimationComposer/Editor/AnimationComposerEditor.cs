using System;
using System.Collections.Generic;
using System.Linq;
using AC.Strcuts;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(AnimationComposer))]
public class AnimationComposerEditor : Editor
{
    private AnimationComposer ac;

    private void OnEnable()
    {
        ac = (AnimationComposer) target;
        rlistDict = new Dictionary<CommandSequence, ReorderableList>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < ac.sequences.Count; i++)
            sequenceGUI(ac.sequences[i]);

        if (GUILayout.Button("New Sequence"))
        {
            Undo.RecordObject(ac, "Undo.AnimationComposer.Seq.Add");
            ac.sequences.Add(new CommandSequence());
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(target);
    }

    private Dictionary<CommandSequence, ReorderableList> rlistDict;

    void sequenceGUI(CommandSequence seq)
    {
        if (!rlistDict.ContainsKey(seq))
        {
            ReorderableList list = new ReorderableList(seq.commands, typeof(Command), true, true, true, true);
            list.elementHeight = 23f;
            list.onAddCallback = v =>
            {
                Undo.RecordObject(ac, "Undo.Commands.Add");
                seq.commands.Add(new Command());
            };
            list.onRemoveCallback = v =>
            {
                Undo.RecordObject(ac, "Undo.Commands.Remove");
                seq.commands.RemoveAt(list.index);
            };
            list.onChangedCallback = v => { EditorUtility.SetDirty(ac); };
            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var cmd = seq.commands[index];
                // commandGUI(rect, cmd);
                onCommandGUI(rect, cmd);
            };

            list.drawHeaderCallback = rect =>
            {
                var rect1 = new Rect(rect);
                rect1.width = 100;
                EditorGUI.LabelField(rect1, "SequenceName");

                var rect3 = new Rect(rect);
                rect3.xMin = rect.xMax - 50;
                rect3.width = 50;
                if (GUI.Button(rect3, "X"))
                {
                    Undo.RecordObject(ac, "Undo.AnimationComposer.Seq.Remove");
                    ac.sequences.Remove(seq);
                }

                var rect2 = new Rect(rect);
                rect2.xMin = rect1.xMax + spacing;
                rect2.width = rect.width - rect1.width - rect3.width - 15;
                seq.name = EditorGUI.TextField(rect2, seq.name);
            };
            rlistDict.Add(seq, list);
        }

        rlistDict[seq].DoLayoutList();
    }

    void onCommandGUI(Rect rect, Command cmd)
    {
        layoutEntries.Clear();

        switch (cmd.type)
        {
            case CommandType.PlayAnimation:
            {
                var isTweenAnim = AnimationComposer.isTweenAnim(cmd.animName);
                beginLayout(rect);
                //type
                layoutElement(100f);
                //target
                layoutElement(80f, -1);
                //name
                layoutElement(60f, -1);
                //tweenTime
                if (isTweenAnim)
                    layoutElement(30f);
                //RP
                layoutElement(35f);
                endLayout();
                cmd.type = drawEnumPopup(cmd.type);
                cmd.target = drawObjectField(cmd.target);
                cmd.animName = drawTextField(cmd.animName);
                if (isTweenAnim)
                    cmd.animTime = drawFloatField(cmd.animTime);
                cmd.reversePlay = drawToggleLeft("RP", cmd.reversePlay);
                break;
            }
            case CommandType.PlayChildAnimation:
            {
                var isTweenAnim = AnimationComposer.isTweenAnim(cmd.animName);
                beginLayout(rect);
                //type
                layoutElement(100f);
                //target
                layoutElement(80f, -1);
                //name
                layoutElement(60f, -1);
                //tweenTime
                if (isTweenAnim)
                    layoutElement(30f);
                //time
                layoutElement(30f);
                //RP
                layoutElement(35f);
                //RC
                layoutElement(35f);
                endLayout();
                cmd.type = drawEnumPopup(cmd.type);
                cmd.target = drawObjectField(cmd.target);
                cmd.animName = drawTextField(cmd.animName);
                if (isTweenAnim)
                    cmd.animTime = drawFloatField(cmd.animTime);
                cmd.time = drawFloatField(cmd.time);
                cmd.reversePlay = drawToggleLeft("RP", cmd.reversePlay);
                cmd.reverse = drawToggleLeft("RC", cmd.reverse);
                break;
            }
            case CommandType.Wait:
                beginLayout(rect);
                layoutElement(100f);
                layoutElement(120f, -1);
                endLayout();
                cmd.type = drawEnumPopup(cmd.type);
                cmd.time = drawFloatField(cmd.time);
                break;
            case CommandType.Deactivate:
            case CommandType.DeactivateChildren:
            case CommandType.WaitChildAnimation:
            case CommandType.WaitAnimation:
                beginLayout(rect);
                layoutElement(100f);
                layoutElement(120f, -1);
                endLayout();
                cmd.type = drawEnumPopup(cmd.type);
                cmd.target = drawObjectField(cmd.target);
                break;
        }
    }

    float spacing = 5;

    struct LayoutEntry
    {
        public Rect rect;
        public float minWidth;
        public float maxWidth;

        public LayoutEntry(float minWidth, float maxWidth)
        {
            this.minWidth = minWidth;
            this.maxWidth = maxWidth;
            this.rect = Rect.zero;
        }
    }

    private Rect elementRect;
    private List<LayoutEntry> layoutEntries = new List<LayoutEntry>();

    void layoutElement(float width)
    {
        layoutEntries.Add(new LayoutEntry(width, width));
    }

    void layoutElement(float minWidth, float maxWidth)
    {
        layoutEntries.Add(new LayoutEntry(minWidth, maxWidth < 0 ? elementRect.width : maxWidth));
    }

    LayoutEntry popLayoutEntry()
    {
        var ent = layoutEntries.First();
        layoutEntries.RemoveAt(0);
        return ent;
    }

    T drawEnumPopup<T>(T selectedEnum) where T : Enum
    {
        return (T) EditorGUI.EnumPopup(popLayoutEntry().rect, selectedEnum);
    }

    float drawFloatField(float value)
    {
        return EditorGUI.FloatField(popLayoutEntry().rect, value);
    }

    string drawTextField(string value)
    {
        return EditorGUI.TextField(popLayoutEntry().rect, value);
    }

    bool drawToggleLeft(string label, bool value)
    {
        return EditorGUI.ToggleLeft(popLayoutEntry().rect, label, value);
    }

    GameObject drawObjectField(GameObject target)
    {
        return EditorGUI.ObjectField(popLayoutEntry().rect, target, typeof(GameObject), true) as GameObject;
    }

    void beginLayout(Rect rect)
    {
        elementRect = rect;
    }

    void endLayout()
    {
        float space = elementRect.width;
        float minWidthSum = layoutEntries.Sum(v => v.minWidth);
        float remainSpace = space - minWidthSum - (layoutEntries.Count - 1) * spacing;
        float accX = elementRect.xMin;
        var notEqualCount = layoutEntries.Count(v => !Mathf.Approximately(v.minWidth, v.maxWidth));

        for (var index = 0; index < layoutEntries.Count; index++)
        {
            var v = layoutEntries[index];
            var rect = new Rect(elementRect);
            rect.xMin = accX;
            rect.yMin += 2f;
            rect.height = 18f;
            if (remainSpace > 0)
                rect.width = Mathf.Clamp(v.minWidth + remainSpace / notEqualCount, v.minWidth, v.maxWidth);
            else
                rect.width = v.minWidth;

            v.rect = rect;

            accX += v.rect.width + spacing;

            layoutEntries[index] = v;
        }
    }
}