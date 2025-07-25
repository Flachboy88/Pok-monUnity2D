using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    ///     The Editor for an AnimatedTile.
    /// </summary>
    [CustomEditor(typeof(AnimatedTile))]
    [MovedFrom(true, "UnityEngine.Tilemaps", "Unity.2D.Tilemap.Extras")]
    public class AnimatedTileEditor : Editor
    {
        private static readonly string k_UndoName = L10n.Tr("Change AnimatedTile");

        private List<Sprite> dragAndDropSprites;

        private SerializedProperty m_AnimatedSprites;

        private ReorderableList reorderableList;

        private AnimatedTile tile => target as AnimatedTile;

        private bool dragAndDropActive =>
            dragAndDropSprites != null
            && dragAndDropSprites.Count > 0;

        SerializedProperty playModeProp;
        SerializedProperty triggerPlayProp;

        private void OnEnable()
        {
            playModeProp = serializedObject.FindProperty("playMode");
            triggerPlayProp = serializedObject.FindProperty("triggerPlay");

            reorderableList = new ReorderableList(tile.m_AnimatedSprites, typeof(Sprite), true, true, true, true);
            reorderableList.drawHeaderCallback = OnDrawHeader;
            reorderableList.drawElementCallback = OnDrawElement;
            reorderableList.elementHeightCallback = GetElementHeight;
            reorderableList.onAddCallback = OnAddElement;
            reorderableList.onRemoveCallback = OnRemoveElement;
            reorderableList.onReorderCallback = OnReorderElement;

            m_AnimatedSprites = serializedObject.FindProperty("m_AnimatedSprites");
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, Styles.orderAnimatedTileSpritesInfo);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (tile.m_AnimatedSprites != null && index < tile.m_AnimatedSprites.Length)
            {
                var spriteName = tile.m_AnimatedSprites[index] != null ? tile.m_AnimatedSprites[index].name : "Null";
                tile.m_AnimatedSprites[index] = (Sprite)EditorGUI.ObjectField(rect
                    , $"Sprite {index + 1}: {spriteName}"
                    , tile.m_AnimatedSprites[index]
                    , typeof(Sprite)
                    , false);
            }
        }

        private float GetElementHeight(int index)
        {
            return 3 * EditorGUI.GetPropertyHeight(SerializedPropertyType.ObjectReference,
                null);
        }

        private void OnAddElement(ReorderableList list)
        {
            var count = tile.m_AnimatedSprites != null ? tile.m_AnimatedSprites.Length + 1 : 1;
            ResizeAnimatedSpriteList(count);

            if (list.index == 0 || list.index < list.count)
            {
                Array.Copy(tile.m_AnimatedSprites, list.index + 1, tile.m_AnimatedSprites, list.index + 2,
                    list.count - list.index - 1);
                tile.m_AnimatedSprites[list.index + 1] = null;
                if (list.IsSelected(list.index))
                    list.index += 1;
            }
            else
            {
                tile.m_AnimatedSprites[count - 1] = null;
            }
        }

        private void OnRemoveElement(ReorderableList list)
        {
            if (tile.m_AnimatedSprites != null && tile.m_AnimatedSprites.Length > 0 &&
                list.index < tile.m_AnimatedSprites.Length)
            {
                var sprites = tile.m_AnimatedSprites.ToList();
                sprites.RemoveAt(list.index);
                tile.m_AnimatedSprites = sprites.ToArray();
            }
        }

        private void OnReorderElement(ReorderableList list)
        {
            // Fix for 2020.1, which does not track changes when reordering in the list
            EditorUtility.SetDirty(tile);
        }

        private void DisplayClipboardText(GUIContent clipboardText, Rect position)
        {
            var old = GUI.color;
            GUI.color = Color.gray;
            var infoSize = GUI.skin.label.CalcSize(clipboardText);
            var rect = new Rect(position.center.x - infoSize.x * .5f
                , position.center.y - infoSize.y * .5f
                , infoSize.x
                , infoSize.y);
            GUI.Label(rect, clipboardText);
            GUI.color = old;
        }

        private void DragAndDropClear()
        {
            dragAndDropSprites = null;
            DragAndDrop.visualMode = DragAndDropVisualMode.None;
            Event.current.Use();
        }

        private static List<Sprite> GetSpritesFromTexture(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();

            foreach (var asset in assets)
                if (asset is Sprite)
                    sprites.Add(asset as Sprite);

            return sprites;
        }

        private static List<Sprite> GetValidSingleSprites(Object[] objects)
        {
            var result = new List<Sprite>();
            foreach (var obj in objects)
                if (obj is Sprite)
                {
                    result.Add(obj as Sprite);
                }
                else if (obj is Texture2D)
                {
                    var texture = obj as Texture2D;
                    var sprites = GetSpritesFromTexture(texture);
                    if (sprites.Count > 0) result.AddRange(sprites);
                }

            return result;
        }

        private void HandleDragAndDrop(Rect guiRect)
        {
            if (DragAndDrop.objectReferences.Length == 0 || !guiRect.Contains(Event.current.mousePosition))
                return;

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    {
                        dragAndDropSprites = GetValidSingleSprites(DragAndDrop.objectReferences);
                        if (dragAndDropActive)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            Event.current.Use();
                            GUI.changed = true;
                        }
                    }
                    break;
                case EventType.DragPerform:
                    {
                        if (!dragAndDropActive)
                            return;

                        Undo.RegisterCompleteObjectUndo(tile, "Drag and Drop to Animated Tile");
                        ResizeAnimatedSpriteList(dragAndDropSprites.Count);
                        Array.Copy(dragAndDropSprites.ToArray(), tile.m_AnimatedSprites, dragAndDropSprites.Count);
                        DragAndDropClear();
                        GUI.changed = true;
                        EditorUtility.SetDirty(tile);
                        GUIUtility.ExitGUI();
                    }
                    break;
                case EventType.Repaint:
                    // Handled in Render()
                    break;
            }

            if (Event.current.type == EventType.DragExited ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
                DragAndDropClear();
        }

        /// <summary>
        ///     Draws an Inspector for the AnimatedTile.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Undo.RecordObject(tile, k_UndoName);

            EditorGUI.BeginChangeCheck();

            // Nombre de sprites animés
            var count = EditorGUILayout.DelayedIntField(
                "Number of Animated Sprites",
                tile.m_AnimatedSprites != null ? tile.m_AnimatedSprites.Length : 0
            );
            if (count < 0) count = 0;

            if (tile.m_AnimatedSprites == null || tile.m_AnimatedSprites.Length != count)
                ResizeAnimatedSpriteList(count);

            // Zone drag & drop pour sprites si aucun sprite
            if (count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 5);
                HandleDragAndDrop(rect);
                EditorGUI.DrawRect(rect,
                    dragAndDropActive && rect.Contains(Event.current.mousePosition) ? Color.white : Color.black);
                var innerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
                EditorGUI.DrawRect(innerRect,
                    EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : (Color)new Color32(194, 194, 194, 255));
                DisplayClipboardText(Styles.emptyAnimatedTileInfo, rect);
                GUILayout.Space(rect.height);
                EditorGUILayout.Space();
            }

            // Liste reorderable des sprites
            if (reorderableList != null)
            {
                var tileCount = tile.m_AnimatedSprites != null ? tile.m_AnimatedSprites.Length : 0;
                if (reorderableList.list == null || reorderableList.count != tileCount)
                    reorderableList.list = tile.m_AnimatedSprites;
                reorderableList.DoLayoutList();
            }

            // Vitesse, frame de départ, collider, flags...
            using (new EditorGUI.DisabledScope(tile.m_AnimatedSprites == null || tile.m_AnimatedSprites.Length == 0))
            {
                var minSpeed = EditorGUILayout.FloatField(Styles.minimumSpeedLabel, tile.m_MinSpeed);
                var maxSpeed = EditorGUILayout.FloatField(Styles.maximumSpeedLabel, tile.m_MaxSpeed);
                minSpeed = Mathf.Max(0f, minSpeed);
                maxSpeed = Mathf.Max(minSpeed, maxSpeed);
                tile.m_MinSpeed = minSpeed;
                tile.m_MaxSpeed = maxSpeed;

                using (new EditorGUI.DisabledScope(tile.m_AnimatedSprites == null
                    || (0 < tile.m_AnimationStartFrame && tile.m_AnimationStartFrame <= tile.m_AnimatedSprites.Length)))
                {
                    tile.m_AnimationStartTime =
                        EditorGUILayout.FloatField(Styles.startTimeLabel, tile.m_AnimationStartTime);
                }

                tile.m_AnimationStartFrame =
                    EditorGUILayout.IntField(Styles.startFrameLabel, tile.m_AnimationStartFrame);
                tile.m_TileColliderType =
                    (Tile.ColliderType)EditorGUILayout.EnumPopup(Styles.colliderTypeLabel, tile.m_TileColliderType);
#if UNITY_2022_2_OR_NEWER
        tile.m_TileAnimationFlags =
            (TileAnimationFlags)EditorGUILayout.EnumFlagsField(Styles.flagsLabel, tile.m_TileAnimationFlags);
#endif
            }

            EditorGUILayout.Space();

            if (playModeProp != null)
                EditorGUILayout.PropertyField(playModeProp, new GUIContent("Play Mode"));

            if (playModeProp != null
                && playModeProp.enumValueIndex == (int)AnimatedTile.PlayMode.Trigger
                && triggerPlayProp != null)
            {
                EditorGUILayout.PropertyField(triggerPlayProp, new GUIContent("Trigger Play (runtime)"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(tile);
            }
        }


        private void ResizeAnimatedSpriteList(int count)
        {
            m_AnimatedSprites.arraySize = count;
            serializedObject.ApplyModifiedProperties();
        }

        private static class Styles
        {
            public static readonly GUIContent orderAnimatedTileSpritesInfo =
                EditorGUIUtility.TrTextContent("Place sprites shown based on the order of animation.");

            public static readonly GUIContent emptyAnimatedTileInfo =
                EditorGUIUtility.TrTextContent(
                    "Drag Sprite or Sprite Texture assets \n" +
                    " to start creating an Animated Tile.");

            public static readonly GUIContent minimumSpeedLabel = EditorGUIUtility.TrTextContent("Minimum Speed",
                "The minimum possible speed at which the Animation of the Tile will be played. A speed value will be randomly chosen between the minimum and maximum speed.");

            public static readonly GUIContent maximumSpeedLabel = EditorGUIUtility.TrTextContent("Maximum Speed",
                "The maximum possible speed at which the Animation of the Tile will be played. A speed value will be randomly chosen between the minimum and maximum speed.");

            public static readonly GUIContent startTimeLabel = EditorGUIUtility.TrTextContent("Start Time",
                "The starting time of this Animated Tile. This allows you to start the Animation from a particular time.");

            public static readonly GUIContent startFrameLabel = EditorGUIUtility.TrTextContent("Start Frame",
                "The starting frame of this Animated Tile. This allows you to start the Animation from a particular Sprite in the list of Animated Sprites.");

            public static readonly GUIContent colliderTypeLabel =
                EditorGUIUtility.TrTextContent("Collider Type", "The Collider Shape generated by the Tile.");

            public static readonly GUIContent flagsLabel =
                EditorGUIUtility.TrTextContent("Flags", "Flags for controlling the Tile Animation.");
        }
    }
}