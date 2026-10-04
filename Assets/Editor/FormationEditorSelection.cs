using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Bộ điều phối lựa chọn Slot phi thuyền đồng bộ (Synchronized Slot Selection).
    /// Giữ kết nối thời gian thực 2 chiều giữa:
    /// 1. 2D Frontal HUD Inspector (FormationPatternEditor.cs).
    /// 2. 3D Scene View Handles (FormationSceneViewPreviewer.cs).
    /// </summary>
    [InitializeOnLoad]
    public static class FormationEditorSelection
    {
        private static FormationPattern _activePattern;
        private static int _selectedSlotIndex = -1;

        public static FormationPattern ActivePattern => _activePattern;
        public static int SelectedSlotIndex => _selectedSlotIndex;

        public static void Select(FormationPattern pattern, int slotIndex)
        {
            _activePattern = pattern;
            _selectedSlotIndex = slotIndex;
        }

        public static void Clear()
        {
            _selectedSlotIndex = -1;
        }

        public static bool IsSelected(FormationPattern pattern, int slotIndex)
        {
            return _activePattern == pattern && _selectedSlotIndex == slotIndex;
        }
    }
}
