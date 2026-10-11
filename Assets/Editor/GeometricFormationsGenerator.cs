using System.IO;
using UnityEditor;
using UnityEngine;
using VirtualRail;

namespace VirtualRail.EditorScripts
{
    /// <summary>
    /// Công cụ Editor 1-click tự động sinh 20 Mẫu Đội Hình Hình Học Vũ Trụ (Geometric Formations)
    /// theo đúng đặc tả tài liệu GEOMETRIC_FORMATIONS_SPEC.md (0 GC Alloc, Data-Driven).
    /// </summary>
    public static class GeometricFormationsGenerator
    {
        private const string OUTPUT_FOLDER = "Assets/Scripts/VirtualRail/Formations_Geometric";

        [MenuItem("Tools/Virtual Rail/Formations/[1] Sinh 20 Mẫu Đội Hình Hình Học (Generate 20 Assets)", false, 120)]
        public static void GenerateAll20Formations()
        {
            if (!Directory.Exists(OUTPUT_FOLDER))
            {
                Directory.CreateDirectory(OUTPUT_FOLDER);
                AssetDatabase.Refresh();
            }

            // Tải các Motion Profile dựng sẵn nếu có
            var profileSinusoid = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_HorizontalSinusoid_Sweep.asset");
            var profileVertical = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_VerticalUndulation_Pulse.asset");
            var profileCorkscrew = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Corkscrew_Spiral3D.asset");
            var profileFigure8 = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_FigureEight_Infinity.asset");
            var profileLissajous = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Lissajous_Cosmic.asset");
            var profilePinwheel = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Pinwheel_CentroidSpin.asset");
            var profileStationary = AssetDatabase.LoadAssetAtPath<FormationMotionProfile>("Assets/Scripts/VirtualRail/MotionProfile_Stationary_Guard.asset");

            int createdCount = 0;

            // 1. Tam giác thường
            CreateOrUpdatePattern("Formation_01_TamGiacThuong", "Tam Giác Thường (Scalene Triangle - 5 Ships)",
                profileSinusoid, FormationEntryStyle.FlankIn, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Leader", new Vector3(2.0f, 7.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.15f),
                    Slot("Left Incline", new Vector3(-4.0f, 2.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Far Left Corner", new Vector3(-10.0f, -5.0f, -2.0f), EnemyArchetype.Flanker, false, 1.0f),
                    Slot("Base Center", new Vector3(1.0f, -6.0f, -1.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Right Corner", new Vector3(11.0f, -3.0f, -2.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // 2. Tam giác đều
            CreateOrUpdatePattern("Formation_02_TamGiacDeu", "Tam Giác Đều (Equilateral Delta - 6 Ships)",
                profileStationary, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Apex Leader", new Vector3(0.0f, 8.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Left Flank", new Vector3(-5.0f, 0.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Right Flank", new Vector3(5.0f, 0.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Bottom Left", new Vector3(-10.0f, -7.0f, -3.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom Center", new Vector3(0.0f, -7.0f, -3.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Bottom Right", new Vector3(10.0f, -7.0f, -3.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 3. Tam giác vuông
            CreateOrUpdatePattern("Formation_03_TamGiacVuong", "Tam Giác Vuông (Right Wing Wedge - 5 Ships)",
                profileVertical, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Right Angle Leader", new Vector3(-8.0f, -6.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Vertical Top", new Vector3(-8.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Vertical Mid", new Vector3(-8.0f, 0.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Horizontal End", new Vector3(10.0f, -6.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Hypotenuse Mid", new Vector3(1.0f, 0.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // 4. Hình vuông
            CreateOrUpdatePattern("Formation_04_HinhVuong", "Hình Vuông (Iron Square Phalanx - 8 Ships)",
                profileCorkscrew, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Citadel Core", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("North-West Corner", new Vector3(-8.0f, 8.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("North Mid", new Vector3(0.0f, 8.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("North-East Corner", new Vector3(8.0f, 8.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("East Mid", new Vector3(8.0f, 0.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("South Mid", new Vector3(0.0f, -8.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("South-West Corner", new Vector3(-8.0f, -8.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("West Mid", new Vector3(-8.0f, 0.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // 5. Hình chữ nhật
            CreateOrUpdatePattern("Formation_05_HinhChuNhat", "Hình Chữ Nhật (Wide Wall Screen - 8 Ships)",
                profileSinusoid, FormationEntryStyle.OverheadDive, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Screen Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top-Left Corner", new Vector3(-12.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top-Center", new Vector3(0.0f, 6.0f, -0.5f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Top-Right Corner", new Vector3(12.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Right-Center", new Vector3(12.0f, 0.0f, -1.5f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Bottom-Right Corner", new Vector3(12.0f, -6.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom-Left Corner", new Vector3(-12.0f, -6.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Left-Center", new Vector3(-12.0f, 0.0f, -1.5f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // 6. Hình bình hành
            CreateOrUpdatePattern("Formation_06_HinhBinhHanh", "Hình Bình Hành (Skewed Parallelogram - 7 Ships)",
                profileLissajous, FormationEntryStyle.RearFlyby, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Centroid Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.15f),
                    Slot("Top-Left", new Vector3(-4.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Top-Right", new Vector3(6.0f, 6.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right-Mid", new Vector3(5.0f, 0.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Bottom-Right", new Vector3(4.0f, -6.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom-Left", new Vector3(-6.0f, -6.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Left-Mid", new Vector3(-5.0f, 0.0f, -1.5f), EnemyArchetype.Interceptor, false, 1.0f)
                });
            createdCount++;

            // 7. Hình thoi
            CreateOrUpdatePattern("Formation_07_HinhThoi", "Hình Thoi (Diamond Guard Elite - 5 Ships)",
                profileFigure8, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("North Peak", new Vector3(0.0f, 8.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("South Peak", new Vector3(0.0f, -8.0f, -1.5f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("West Wing", new Vector3(-14.0f, 0.0f, -2.5f), EnemyArchetype.Flanker, false, 1.0f),
                    Slot("East Wing", new Vector3(14.0f, 0.0f, -2.5f), EnemyArchetype.Flanker, false, 1.0f)
                });
            createdCount++;

            // 8. Hình thang
            CreateOrUpdatePattern("Formation_08_HinhThang", "Hình Thang (Broad Trapezoid - 6 Ships)",
                profileSinusoid, FormationEntryStyle.FlankIn, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Centroid Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.15f),
                    Slot("Top Minor Left", new Vector3(-5.0f, 5.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Top Minor Right", new Vector3(3.0f, 5.0f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Base Left", new Vector3(-12.0f, -6.0f, -2.5f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Base Mid", new Vector3(-2.0f, -6.0f, -2.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Base Right", new Vector3(10.0f, -6.0f, -2.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            // 9. Hình thang cân
            CreateOrUpdatePattern("Formation_09_HinhThangCan", "Hình Thang Cân (Isosceles Bastion - 7 Ships)",
                profileStationary, FormationEntryStyle.OverheadDive, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Crown Leader", new Vector3(0.0f, 6.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top Minor Left", new Vector3(-5.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top Minor Right", new Vector3(5.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Left Waist", new Vector3(-8.0f, 0.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Waist", new Vector3(8.0f, 0.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Base Far Left", new Vector3(-12.0f, -6.0f, -3.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Base Far Right", new Vector3(12.0f, -6.0f, -3.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // 10. Hình thang vuông
            CreateOrUpdatePattern("Formation_10_HinhThangVuong", "Hình Thang Vuông (Right Bastion Rampart - 6 Ships)",
                profileVertical, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Wall Center Leader", new Vector3(-8.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.2f),
                    Slot("Top Wall", new Vector3(-8.0f, 6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Top Ridge", new Vector3(-1.0f, 6.0f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Incline", new Vector3(4.0f, 0.0f, -2.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Bottom Wall", new Vector3(-8.0f, -6.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Base Right End", new Vector3(10.0f, -6.0f, -2.5f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 11. Ngũ giác đều
            CreateOrUpdatePattern("Formation_11_NguGiacDeu", "Ngũ Giác Đều (Pentagram Citadel - 6 Ships)",
                profilePinwheel, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Citadel Core", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("North Peak (90 deg)", new Vector3(0.0f, 10.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("North-East (18 deg)", new Vector3(9.51f, 3.09f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("South-East (-54 deg)", new Vector3(5.88f, -8.09f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("South-West (-126 deg)", new Vector3(-5.88f, -8.09f, -1.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("North-West (162 deg)", new Vector3(-9.51f, 3.09f, -1.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 12. Lục giác đều
            CreateOrUpdatePattern("Formation_12_LucGiacDeu", "Lục Giác Đều (Hexagonal Hive - 7 Ships)",
                profilePinwheel, FormationEntryStyle.OverheadDive, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Hive Queen Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.3f),
                    Slot("Peak North (90 deg)", new Vector3(0.0f, 10.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("East-North (30 deg)", new Vector3(8.66f, 5.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("East-South (-30 deg)", new Vector3(8.66f, -5.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Peak South (-90 deg)", new Vector3(0.0f, -10.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("West-South (-150 deg)", new Vector3(-8.66f, -5.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("West-North (150 deg)", new Vector3(-8.66f, 5.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f)
                });
            createdCount++;

            // 13. Bát giác đều
            CreateOrUpdatePattern("Formation_13_BatGiacDeu", "Bát Giác Đều (Octagonal Ring - 9 Ships)",
                profileCorkscrew, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Energy Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("North (90 deg)", new Vector3(0.0f, 10.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("North-East (45 deg)", new Vector3(7.07f, 7.07f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("East (0 deg)", new Vector3(10.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("South-East (-45 deg)", new Vector3(7.07f, -7.07f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("South (-90 deg)", new Vector3(0.0f, -10.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("South-West (-135 deg)", new Vector3(-7.07f, -7.07f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("West (180 deg)", new Vector3(-10.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("North-West (135 deg)", new Vector3(-7.07f, 7.07f, -1.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 14. Thập giác đều
            CreateOrUpdatePattern("Formation_14_ThapGiacDeu", "Thập Giác Đều (Decagon Decashield - 11 Ships)",
                profileFigure8, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Decagon Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.3f),
                    Slot("Peak 1 (90 deg)", new Vector3(0.0f, 11.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Peak 2 (54 deg)", new Vector3(6.47f, 8.9f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Peak 3 (18 deg)", new Vector3(10.46f, 3.4f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Peak 4 (-18 deg)", new Vector3(10.46f, -3.4f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Peak 5 (-54 deg)", new Vector3(6.47f, -8.9f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Peak 6 (-90 deg)", new Vector3(0.0f, -11.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Peak 7 (-126 deg)", new Vector3(-6.47f, -8.9f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Peak 8 (-162 deg)", new Vector3(-10.46f, -3.4f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Peak 9 (162 deg)", new Vector3(-10.46f, 3.4f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Peak 10 (126 deg)", new Vector3(-6.47f, 8.9f, -1.0f), EnemyArchetype.Flanker, false, 0.9f)
                });
            createdCount++;

            // 15. n-giác đều tổng quát (Thập Nhị Giác Đều - 12 Đỉnh)
            CreateOrUpdatePattern("Formation_15_NGiacDeu_12Dinh", "n-Giác Đều (Star Dodecahedron - 13 Ships)",
                profileLissajous, FormationEntryStyle.OverheadDive, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Dodeca Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.35f),
                    Slot("Vertex (90 deg)", new Vector3(0.0f, 12.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Vertex (60 deg)", new Vector3(6.0f, 10.39f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Vertex (30 deg)", new Vector3(10.39f, 6.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Vertex (0 deg)", new Vector3(12.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Vertex (-30 deg)", new Vector3(10.39f, -6.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Vertex (-60 deg)", new Vector3(6.0f, -10.39f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Vertex (-90 deg)", new Vector3(0.0f, -12.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Vertex (-120 deg)", new Vector3(-6.0f, -10.39f, -1.0f), EnemyArchetype.Flanker, false, 0.9f),
                    Slot("Vertex (-150 deg)", new Vector3(-10.39f, -6.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Vertex (180 deg)", new Vector3(-12.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Vertex (150 deg)", new Vector3(-10.39f, 6.0f, -1.0f), EnemyArchetype.Interceptor, false, 0.9f),
                    Slot("Vertex (120 deg)", new Vector3(-6.0f, 10.39f, -1.0f), EnemyArchetype.Flanker, false, 0.9f)
                });
            createdCount++;

            // 16. Hình tròn
            CreateOrUpdatePattern("Formation_16_HinhTron", "Hình Tròn (Orbital Halo Swarm - 9 Ships)",
                profileCorkscrew, FormationEntryStyle.RearFlyby, LeaderDeathReaction.RetreatWarp,
                new FormationSlotDefinition[]
                {
                    Slot("Halo Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Halo (90 deg)", new Vector3(0.0f, 9.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Halo (45 deg)", new Vector3(6.36f, 6.36f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Halo (0 deg)", new Vector3(9.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Halo (-45 deg)", new Vector3(6.36f, -6.36f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Halo (-90 deg)", new Vector3(0.0f, -9.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Halo (-135 deg)", new Vector3(-6.36f, -6.36f, -1.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Halo (180 deg)", new Vector3(-9.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Halo (135 deg)", new Vector3(-6.36f, 6.36f, -1.0f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 17. Hình Ellipse
            CreateOrUpdatePattern("Formation_17_HinhEllipse", "Hình Ellipse (Elliptic Eclipse - 9 Ships)",
                profileLissajous, FormationEntryStyle.OverheadDive, LeaderDeathReaction.PanicScatter,
                new FormationSlotDefinition[]
                {
                    Slot("Focus Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Top (90 deg)", new Vector3(0.0f, 7.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Upper Right (45 deg)", new Vector3(9.90f, 4.95f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("East (0 deg)", new Vector3(14.0f, 0.0f, -2.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Lower Right (-45 deg)", new Vector3(9.90f, -4.95f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Bottom (-90 deg)", new Vector3(0.0f, -7.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Lower Left (-135 deg)", new Vector3(-9.90f, -4.95f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("West (180 deg)", new Vector3(-14.0f, 0.0f, -2.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Upper Left (135 deg)", new Vector3(-9.90f, 4.95f, -1.5f), EnemyArchetype.Flanker, false, 0.95f)
                });
            createdCount++;

            // 18. Hình quạt tròn
            CreateOrUpdatePattern("Formation_18_HinhQuatTron", "Hình Quạt Tròn (Radial Fan Sweep - 7 Ships)",
                profileSinusoid, FormationEntryStyle.FlankIn, LeaderDeathReaction.KamikazeCharge,
                new FormationSlotDefinition[]
                {
                    Slot("Fan Pivot Leader", new Vector3(0.0f, -6.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Spoke", new Vector3(-4.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Right Spoke", new Vector3(4.0f, 0.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Arc Far Left", new Vector3(-8.0f, 5.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Arc Left", new Vector3(-4.0f, 7.0f, -2.0f), EnemyArchetype.Scout, false, 0.95f),
                    Slot("Arc Crown", new Vector3(0.0f, 8.0f, -2.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Arc Right", new Vector3(4.0f, 7.0f, -2.0f), EnemyArchetype.Scout, false, 0.95f)
                });
            createdCount++;

            // 19. Hình vành khuyên
            CreateOrUpdatePattern("Formation_19_HinhVanhKhuyen", "Hình Vành Khuyên (Dual Ring Annulus - 11 Ships)",
                profilePinwheel, FormationEntryStyle.FrontalHorizon, LeaderDeathReaction.MaintainFormation,
                new FormationSlotDefinition[]
                {
                    Slot("Annulus Core Leader", new Vector3(0.0f, 0.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.3f),
                    Slot("Inner Ring North", new Vector3(0.0f, 5.0f, -0.8f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Inner Ring East", new Vector3(5.0f, 0.0f, -0.8f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Inner Ring South", new Vector3(0.0f, -5.0f, -0.8f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Inner Ring West", new Vector3(-5.0f, 0.0f, -0.8f), EnemyArchetype.ShieldBearer, false, 1.0f),
                    Slot("Outer Ring North", new Vector3(0.0f, 11.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Outer Ring North-East", new Vector3(9.53f, 5.5f, -2.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Outer Ring South-East", new Vector3(9.53f, -5.5f, -2.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Outer Ring South", new Vector3(0.0f, -11.0f, -2.0f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Outer Ring South-West", new Vector3(-9.53f, -5.5f, -2.0f), EnemyArchetype.Interceptor, false, 0.95f),
                    Slot("Outer Ring North-West", new Vector3(-9.53f, 5.5f, -2.0f), EnemyArchetype.Interceptor, false, 0.95f)
                });
            createdCount++;

            // 20. Hình viên phân
            CreateOrUpdatePattern("Formation_20_HinhVienPhan", "Hình Viên Phân (Crescent Bow Segment - 7 Ships)",
                profileVertical, FormationEntryStyle.OverheadDive, LeaderDeathReaction.BreakShieldAndEngage,
                new FormationSlotDefinition[]
                {
                    Slot("Bowstring Center Leader", new Vector3(0.0f, 2.0f, 0.0f), EnemyArchetype.SquadLeader, true, 1.25f),
                    Slot("Left Notch", new Vector3(-10.0f, 2.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Left Arch", new Vector3(-5.0f, 6.0f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Crown Peak", new Vector3(0.0f, 7.5f, -2.0f), EnemyArchetype.Interceptor, false, 1.0f),
                    Slot("Right Arch", new Vector3(5.0f, 6.0f, -1.5f), EnemyArchetype.Flanker, false, 0.95f),
                    Slot("Right Notch", new Vector3(10.0f, 2.0f, -1.0f), EnemyArchetype.ShieldBearer, false, 1.05f),
                    Slot("Left String Guard", new Vector3(-5.0f, 2.0f, -0.5f), EnemyArchetype.ShieldBearer, false, 1.0f)
                });
            createdCount++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Thành công!",
                    $"Đã sinh thành công {createdCount} / 20 mẫu đội hình hình học tại:\n{OUTPUT_FOLDER}\n\nBạn có thể mở Formation Pattern Editor hoặc kéo thả vào StageData ngay bây giờ!",
                    "OK");
            }
            Debug.Log($"<color=green><b>[GEOMETRIC FORMATIONS]</b></color> Đã sinh hoàn tất {createdCount} Mẫu Đội Hình Hình Học tại {OUTPUT_FOLDER}!");
        }

        [MenuItem("Tools/Virtual Rail/Formations/[2] Xóa Các Mẫu Hình Học Đã Sinh (Undo Generated Assets)", false, 121)]
        public static void DeleteAllGeneratedFormations()
        {
            if (Directory.Exists(OUTPUT_FOLDER))
            {
                bool confirm = Application.isBatchMode || EditorUtility.DisplayDialog("Xác nhận hoàn nguyên (Undo)",
                    $"Bạn có chắc chắn muốn xóa thư mục {OUTPUT_FOLDER} và toàn bộ 20 file FormationPattern hình học đã sinh?",
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
                    Debug.Log($"<color=orange><b>[UNDO FORMATIONS]</b></color> Đã hoàn nguyên, xóa sạch thư mục {OUTPUT_FOLDER}!");
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
