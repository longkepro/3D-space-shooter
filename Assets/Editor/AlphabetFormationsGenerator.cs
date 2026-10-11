using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Công cụ Editor 1-click tự động sinh 26 Mẫu Đội Hình Chữ Cái Tiếng Anh (A - Z)
    /// theo đúng đặc tả tài liệu ALPHABET_FORMATIONS_SPEC.md (0 GC Alloc, Data-Driven).
    /// </summary>
    public static class AlphabetFormationsGenerator
    {
        private const string OUTPUT_FOLDER = "Assets/Scripts/VirtualRail/Formations_Alphabet";

        [MenuItem("Tools/Virtual Rail/Formations/[Alphabet] Sinh 26 Mẫu Đội Hình Chữ Cái A-Z", false, 130)]
        public static void GenerateAll26AlphabetFormations()
        {
            if (!Directory.Exists(OUTPUT_FOLDER))
            {
                Directory.CreateDirectory(OUTPUT_FOLDER);
                AssetDatabase.Refresh();
            }

            var profileSinusoid = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_HorizontalSinusoid_Sweep.asset");
            var profileVertical = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_VerticalUndulation_Pulse.asset");
            var profileCorkscrew = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Corkscrew_Spiral3D.asset");
            var profileFigure8 = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_FigureEight_Infinity.asset");
            var profileLissajous = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Lissajous_Cosmic.asset");
            var profilePinwheel = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Pinwheel_CentroidSpin.asset");
            var profileStationary = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Stationary_Guard.asset");

            int createdCount = 0;

            // A
            CreateOrUpdatePattern("Formation_A", "Letter A Formation (8 Ships)",
                profileStationary, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Leader", new Vector3(0.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Upper Left", new Vector3(-3.5f, 3.5f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Upper Right", new Vector3(3.5f, 3.5f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Crossbar Left", new Vector3(-5.0f, 0.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Crossbar Mid", new Vector3(0.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Crossbar Right", new Vector3(5.0f, 0.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Foot Left", new Vector3(-7.5f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Foot Right", new Vector3(7.5f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // B
            CreateOrUpdatePattern("Formation_B", "Letter B Formation (9 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Spine Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Mid Spine", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Bottom Spine", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top Bar", new Vector3(-1.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Upper Arch", new Vector3(4.5f, 4.0f, -1.2f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Center Cusp", new Vector3(-1.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Lower Arch", new Vector3(4.5f, -3.5f, -1.2f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Bar", new Vector3(-1.0f, -7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Apex", new Vector3(5.5f, 0.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            // C
            CreateOrUpdatePattern("Formation_C", "Letter C Formation (7 Ships)",
                profileVertical, FormationEntryStyle.FlankIn, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Top Tip Leader", new Vector3(6.0f, 6.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Top Arch", new Vector3(0.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Upper Left", new Vector3(-6.0f, 4.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Mid Spine", new Vector3(-7.5f, 0.0f, -1.2f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Lower Left", new Vector3(-6.0f, -4.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom Arch", new Vector3(0.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom Tip", new Vector3(6.0f, -6.0f, 0.0f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // D
            CreateOrUpdatePattern("Formation_D", "Letter D Formation (8 Ships)",
                profileCorkscrew, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Spine Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top Arch", new Vector3(0.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Upper Right", new Vector3(6.0f, 4.0f, -1.2f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Far Right Peak", new Vector3(7.5f, 0.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Lower Right", new Vector3(6.0f, -4.0f, -1.2f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Arch", new Vector3(0.0f, -7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // E
            CreateOrUpdatePattern("Formation_E", "Letter E Formation (9 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Top Left Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Upper", new Vector3(-6.0f, 3.5f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Lower", new Vector3(-6.0f, -3.5f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top Arm End", new Vector3(5.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Top Arm Mid", new Vector3(-0.5f, 7.0f, -0.8f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Mid Arm End", new Vector3(2.0f, 0.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Arm End", new Vector3(5.0f, -7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // F
            CreateOrUpdatePattern("Formation_F", "Letter F Formation (7 Ships)",
                profileVertical, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Top Left Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Mid", new Vector3(-6.0f, 1.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top Arm Mid", new Vector3(-0.5f, 7.0f, -0.8f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Top Arm End", new Vector3(5.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Mid Arm Mid", new Vector3(-1.0f, 1.0f, -0.8f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Mid Arm End", new Vector3(3.0f, 1.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // G
            CreateOrUpdatePattern("Formation_G", "Letter G Formation (9 Ships)",
                profileFigure8, FormationEntryStyle.RearFlyby, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Inner Spur Leader", new Vector3(1.0f, -2.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top Arch", new Vector3(0.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top Right", new Vector3(5.0f, 6.0f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Upper Left", new Vector3(-6.0f, 4.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Mid Left", new Vector3(-7.5f, 0.0f, -1.2f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Lower Left", new Vector3(-6.0f, -4.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom Arch", new Vector3(0.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom Right", new Vector3(6.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Right Stem", new Vector3(6.0f, -2.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            // H
            CreateOrUpdatePattern("Formation_H", "Letter H Formation (8 Ships)",
                profileStationary, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Left Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Left Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Crossbar Left", new Vector3(-2.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Crossbar Right", new Vector3(2.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Top", new Vector3(6.0f, 7.0f, 0.0f), EnemyArchetype.Flanker, false, 1.05f),
                    Slot("Right Mid", new Vector3(6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Right Bottom", new Vector3(6.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // I
            CreateOrUpdatePattern("Formation_I", "Letter I Formation (7 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Top Center Leader", new Vector3(0.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top Bar Left", new Vector3(-5.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top Bar Right", new Vector3(5.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Upper", new Vector3(0.0f, 2.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Spine Lower", new Vector3(0.0f, -3.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Bottom Bar Left", new Vector3(-5.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Bar Right", new Vector3(5.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // J
            CreateOrUpdatePattern("Formation_J", "Letter J Formation (6 Ships)",
                profileVertical, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Top Right Leader", new Vector3(4.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Stem Upper", new Vector3(4.0f, 2.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Stem Lower", new Vector3(4.0f, -3.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom Arch", new Vector3(0.0f, -7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Hook Left", new Vector3(-4.0f, -6.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Hook Tip", new Vector3(-5.0f, -2.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // K
            CreateOrUpdatePattern("Formation_K", "Letter K Formation (7 Ships)",
                profileLissajous, FormationEntryStyle.RearFlyby, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Spine Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Upper Diagonal Mid", new Vector3(-1.0f, 3.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Upper Diagonal Tip", new Vector3(6.0f, 7.0f, -1.5f), EnemyArchetype.Flanker, false, 1.0f),
                    Slot("Lower Diagonal Mid", new Vector3(-1.0f, -3.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Lower Diagonal Tip", new Vector3(6.0f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // L
            CreateOrUpdatePattern("Formation_L", "Letter L Formation (5 Ships)",
                profileStationary, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Top Tip Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Spine Upper", new Vector3(-6.0f, 2.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Lower", new Vector3(-6.0f, -3.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Corner Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Foot End", new Vector3(4.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // M
            CreateOrUpdatePattern("Formation_M", "Letter M Formation (9 Ships)",
                profileFigure8, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Center Valley Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Peak", new Vector3(-8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Left Mid Stem", new Vector3(-8.0f, 0.0f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Left Foot", new Vector3(-8.0f, -7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Left Incline", new Vector3(-4.0f, 3.5f, -0.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Incline", new Vector3(4.0f, 3.5f, -0.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Peak", new Vector3(8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Right Mid Stem", new Vector3(8.0f, 0.0f, -0.8f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Right Foot", new Vector3(8.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // N
            CreateOrUpdatePattern("Formation_N", "Letter N Formation (7 Ships)",
                profileLissajous, FormationEntryStyle.RearFlyby, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Left Top Leader", new Vector3(-7.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Mid", new Vector3(-7.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Left Foot", new Vector3(-7.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Diagonal Upper", new Vector3(-2.5f, 2.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Diagonal Lower", new Vector3(2.5f, -2.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Top", new Vector3(7.0f, 7.0f, -0.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Right Foot", new Vector3(7.0f, -7.0f, 0.0f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // O
            CreateOrUpdatePattern("Formation_O", "Letter O Formation (8 Ships)",
                profileCorkscrew, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Top Apex Leader", new Vector3(0.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top-Right", new Vector3(5.5f, 5.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Mid", new Vector3(7.5f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom-Right", new Vector3(5.5f, -5.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Base", new Vector3(0.0f, -7.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom-Left", new Vector3(-5.5f, -5.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Left Mid", new Vector3(-7.5f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top-Left", new Vector3(-5.5f, 5.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // P
            CreateOrUpdatePattern("Formation_P", "Letter P Formation (7 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Spine Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Loop Top Bar", new Vector3(0.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Loop Far Right", new Vector3(5.0f, 3.5f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Loop Mid Bar", new Vector3(0.0f, 0.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Loop Shoulder", new Vector3(3.5f, 6.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f)
                });
            createdCount++;

            // Q
            CreateOrUpdatePattern("Formation_Q", "Letter Q Formation (9 Ships)",
                profileCorkscrew, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Top Apex Leader", new Vector3(0.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top-Right", new Vector3(5.5f, 5.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Mid", new Vector3(7.5f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom-Right", new Vector3(5.5f, -5.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Base", new Vector3(0.0f, -7.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom-Left", new Vector3(-5.5f, -5.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Left Mid", new Vector3(-7.5f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top-Left", new Vector3(-5.5f, 5.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Tail Spur", new Vector3(8.5f, -8.5f, -1.5f), EnemyArchetype.Interceptor, false, 1.05f)
                });
            createdCount++;

            // R
            CreateOrUpdatePattern("Formation_R", "Letter R Formation (8 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Spine Top Leader", new Vector3(-6.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Spine Mid", new Vector3(-6.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Spine Bottom", new Vector3(-6.0f, -7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Loop Top", new Vector3(0.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Loop Apex", new Vector3(5.0f, 3.5f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Loop Base", new Vector3(0.0f, 0.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Stride Mid", new Vector3(2.5f, -3.5f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Stride Foot", new Vector3(6.0f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // S
            CreateOrUpdatePattern("Formation_S", "Letter S Formation (9 Ships)",
                profileSinusoid, FormationEntryStyle.FlankIn, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Upper Spine Leader", new Vector3(-6.0f, 3.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top Left Arch", new Vector3(-4.0f, 7.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Top Crown", new Vector3(0.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top Right Hook", new Vector3(5.0f, 5.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Center Bridge", new Vector3(-2.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Center Mid", new Vector3(2.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Lower Spine", new Vector3(6.0f, -3.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom Base", new Vector3(0.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom Left Hook", new Vector3(-5.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // T
            CreateOrUpdatePattern("Formation_T", "Letter T Formation (7 Ships)",
                profileVertical, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Crown Center Leader", new Vector3(0.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Crossbar Far Left", new Vector3(-8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Crossbar Mid Left", new Vector3(-4.0f, 7.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Crossbar Mid Right", new Vector3(4.0f, 7.0f, -0.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Crossbar Far Right", new Vector3(8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Stem Mid", new Vector3(0.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Stem Base", new Vector3(0.0f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // U
            CreateOrUpdatePattern("Formation_U", "Letter U Formation (7 Ships)",
                profileFigure8, FormationEntryStyle.OverheadDive, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Bottom Center Leader", new Vector3(0.0f, -7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Top", new Vector3(-6.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Left Mid", new Vector3(-6.0f, 0.0f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom Left Curve", new Vector3(-4.5f, -5.5f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom Right Curve", new Vector3(4.5f, -5.5f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Right Mid", new Vector3(6.0f, 0.0f, -0.8f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Right Top", new Vector3(6.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            // V
            CreateOrUpdatePattern("Formation_V", "Letter V Formation (6 Ships)",
                profilePinwheel, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Apex Bottom Leader", new Vector3(0.0f, -7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Mid", new Vector3(-3.5f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Left Wingtip", new Vector3(-7.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Mid", new Vector3(3.5f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Right Wingtip", new Vector3(7.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Rear Guard", new Vector3(0.0f, -3.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // W
            CreateOrUpdatePattern("Formation_W", "Letter W Formation (9 Ships)",
                profileFigure8, FormationEntryStyle.OverheadDive, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Center Peak Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Wingtip", new Vector3(-8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Left Mid", new Vector3(-6.0f, -3.5f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Left Cusp Bottom", new Vector3(-4.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Left Incline Mid", new Vector3(-2.0f, -3.5f, -0.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Incline Mid", new Vector3(2.0f, -3.5f, -0.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Cusp Bottom", new Vector3(4.0f, -7.0f, -1.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Right Mid", new Vector3(6.0f, -3.5f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Wingtip", new Vector3(8.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f)
                });
            createdCount++;

            // X
            CreateOrUpdatePattern("Formation_X", "Letter X Formation (9 Ships)",
                profileLissajous, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Core Center Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.3f),
                    Slot("Top-Left End", new Vector3(-7.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top-Left Mid", new Vector3(-3.5f, 3.5f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Top-Right End", new Vector3(7.0f, 7.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top-Right Mid", new Vector3(3.5f, 3.5f, -0.8f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom-Left End", new Vector3(-7.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Bottom-Left Mid", new Vector3(-3.5f, -3.5f, -0.8f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Bottom-Right End", new Vector3(7.0f, -7.0f, -0.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Bottom-Right Mid", new Vector3(3.5f, -3.5f, -0.8f), EnemyArchetype.Scout, false, 0.95f)
                });
            createdCount++;

            // Y
            CreateOrUpdatePattern("Formation_Y", "Letter Y Formation (7 Ships)",
                profileVertical, FormationEntryStyle.OverheadDive, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Fork Center Leader", new Vector3(0.0f, 1.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Branch Mid", new Vector3(-3.5f, 4.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Left Branch Tip", new Vector3(-7.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Branch Mid", new Vector3(3.5f, 4.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Right Branch Tip", new Vector3(7.0f, 7.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Stem Mid", new Vector3(0.0f, -3.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Stem Base", new Vector3(0.0f, -7.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // Z
            CreateOrUpdatePattern("Formation_Z", "Letter Z Formation (9 Ships)",
                profileSinusoid, FormationEntryStyle.RearFlyby, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Top Center Leader", new Vector3(0.0f, 6.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top-Left End", new Vector3(-7.0f, 6.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top-Right End", new Vector3(7.0f, 6.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Diagonal Upper", new Vector3(3.5f, 2.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Diagonal Center", new Vector3(0.0f, 0.0f, -1.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Diagonal Lower", new Vector3(-3.5f, -2.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom-Left End", new Vector3(-7.0f, -6.0f, -0.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Bottom Center", new Vector3(0.0f, -6.0f, -0.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom-Right End", new Vector3(7.0f, -6.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Thành công!",
                    $"Đã sinh thành công {createdCount} / 26 mẫu đội hình chữ cái A-Z tại:\n{OUTPUT_FOLDER}\n\nBạn có thể mở Formation Pattern Editor hoặc kéo thả vào StageData ngay bây giờ!",
                    "OK");
            }
            Debug.Log($"<color=green><b>[ALPHABET FORMATIONS]</b></color> Đã sinh hoàn tất {createdCount} Mẫu Đội Hình Chữ Cái A-Z tại {OUTPUT_FOLDER}!");
        }

        [MenuItem("Tools/Virtual Rail/Formations/[Alphabet] Xóa Đội Hình Chữ Cái (Undo)", false, 131)]
        public static void DeleteAllAlphabetFormations()
        {
            if (Directory.Exists(OUTPUT_FOLDER))
            {
                bool confirm = Application.isBatchMode || EditorUtility.DisplayDialog("Xác nhận hoàn nguyên (Undo)",
                    $"Bạn có chắc chắn muốn xóa thư mục {OUTPUT_FOLDER} và toàn bộ 26 file FormationPattern chữ cái đã sinh?",
                    "Xác nhận Xóa", "Hủy");

                if (confirm)
                {
                    FileUtil.DeleteFileOrDirectory(OUTPUT_FOLDER);
                    string metaFile = OUTPUT_FOLDER + ".meta";
                    if (File.Exists(metaFile))
                    {
                        FileUtil.DeleteFileOrDirectory(metaFile);
                    }
                    AssetDatabase.Refresh();
                    Debug.Log($"<color=orange><b>[UNDO ALPHABET]</b></color> Đã hoàn nguyên, xóa sạch thư mục {OUTPUT_FOLDER}!");
                }
            }
        }

        private static FormationSlotDefinition Slot(string name, Vector3 offset, EnemyArchetype archetype, bool isLeader, float scale)
        {
            return new FormationSlotDefinition
            {
                slotName = name,
                localOffset = offset,
                preferredArchetype = archetype,
                isLeader = isLeader,
                scaleMultiplier = scale,
                customPrefabOverride = null
            };
        }

        private static void CreateOrUpdatePattern(
            string assetFileName,
            string patternName,
            FormationMotionProfile motionProfile,
            FormationEntryStyle entryStyle,
            LeaderDeathReaction leaderReaction,
            FormationSlotDefinition[] slots)
        {
            string fullPath = $"{OUTPUT_FOLDER}/{assetFileName}.asset";
            FormationPattern pattern = AssetDatabase.LoadAssetAtPath<FormationPattern>(fullPath);
            bool isNew = false;

            if (pattern == null)
            {
                pattern = ScriptableObject.CreateInstance<FormationPattern>();
                isNew = true;
            }

            pattern.patternName = patternName;
            pattern.slots = slots;
            pattern.motionProfile = motionProfile;
            pattern.entryStyle = entryStyle;
            pattern.exitStyle = FormationExitStyle.ClimbOut;
            pattern.exitDuration = 1.8f;
            pattern.assemblyTime = 1.2f;
            pattern.attitudeMode = FormationAttitudeMode.StabilizedBoresight;
            pattern.holdDistance = 65f;
            pattern.maxClosingSpeed = 25f;
            pattern.maxLifetime = 14f;
            pattern.leaderDeathReaction = leaderReaction;

            if (isNew)
            {
                AssetDatabase.CreateAsset(pattern, fullPath);
            }
            else
            {
                EditorUtility.SetDirty(pattern);
            }
        }
    }
}
