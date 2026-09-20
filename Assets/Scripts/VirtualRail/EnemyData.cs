using System;
using UnityEngine;

namespace VirtualRail
{
    public enum EnemyArchetype
    {
        Scout = 0,        // 1 hit, bay lướt, đạn đơn/không bắn, luyện ngắm & farm combo
        Flanker = 1,      // 2 hits, tạt sườn cắt ngang hoặc nhập đội hình, đạn kép
        Interceptor = 2,  // 2 hits từ trước / 1 hit điểm yếu sau đuôi, đạn Dart tốc độ cao
        SquadLeader = 3,  // 4 hits / 1 Charge Shot, chỉ huy đội hình, chết kích hoạt Panic Scatter
        ShieldBearer = 4  // 2 hits khi mất khiên, bật lá chắn năng lượng bảo vệ khi vào đội hình có Chỉ Huy
    }

    public enum FlankerTactic
    {
        StrafingRun = 0,  // Tạt ngang qua màn hình xả đạn rồi thoát ly luôn (không nhập đội hình)
        JoinFormation = 1 // Tạt sườn vào rồi khóa vào vị trí đội hình (chữ V, hàng ngang)
    }

    /// <summary>
    /// Định nghĩa cấu hình tham số cho từng chủng loại kẻ địch (Enemy Archetype)
    /// Hỗ trợ cả tạo ScriptableObject Asset lẫn dùng Preset mặc định trong mã nguồn.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Virtual Rail/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public EnemyArchetype archetype = EnemyArchetype.Scout;
        public string displayName = "Scout Drone";

        [Header("Attributes & Scoring")]
        [Tooltip("Lượng máu của kẻ địch (Laser chuẩn = 20 DMG)")]
        public int maxHealth = 20;
        [Tooltip("Điểm thưởng khi tiêu diệt")]
        public int scoreValue = 100;
        [Tooltip("Điểm ngân sách đe dọa (Threat Cost cho Spawner)")]
        public int threatCost = 10;

        [Header("Movement")]
        public float movementSpeed = 20f;
        public float turnSpeed = 0.5f;

        [Header("Combat")]
        public float fireCooldown = 1.5f;
        public float projectileSpeed = 20f;
        public int burstCount = 1;
        public float burstInterval = 0.15f;

        [Header("Weakspot Mechanics (TDD v1.0.0 Phần II.1)")]
        [Tooltip("Có điểm yếu sau ống xả động cơ không")]
        public bool hasRearWeakSpot = false;
        [Tooltip("Hệ số nhân sát thương khi bắn trúng điểm yếu (chuẩn 200%)")]
        public float weakSpotMultiplier = 2.0f;

        [Header("Shield Mechanics")]
        [Tooltip("Có khả năng bật lá chắn bảo vệ khi gia nhập đội hình có Chỉ Huy không")]
        public bool hasFormationShield = false;

        [Header("Flanker Specific")]
        public FlankerTactic flankerTactic = FlankerTactic.StrafingRun;

        /// <summary>
        /// Bộ thiết lập mặc định cho từng Archetype theo chuẩn TDD v1.0.0.
        /// Sử dụng trực tiếp trong mã nguồn mà không bắt buộc phải tạo asset thủ công.
        /// </summary>
        public static EnemyData CreateDefaultPreset(EnemyArchetype type)
        {
            var data = CreateInstance<EnemyData>();
            data.archetype = type;

            switch (type)
            {
                case EnemyArchetype.Scout:
                    data.displayName = "Scout Drone (Enemy 0)";
                    data.maxHealth = 20; // 1 laser hit
                    data.scoreValue = 100;
                    data.threatCost = 10;
                    data.movementSpeed = 20f;
                    data.turnSpeed = 0.5f;
                    data.fireCooldown = 2.0f;
                    data.projectileSpeed = 20f;
                    data.burstCount = 1;
                    data.hasRearWeakSpot = false;
                    break;

                case EnemyArchetype.Flanker:
                    data.displayName = "Flanker Jet (Enemy 1)";
                    data.maxHealth = 40; // 2 laser hits
                    data.scoreValue = 250;
                    data.threatCost = 25;
                    data.movementSpeed = 30f;
                    data.turnSpeed = 1.0f;
                    data.fireCooldown = 1.2f;
                    data.projectileSpeed = 30f;
                    data.burstCount = 2; // Bắn chùm kép
                    data.burstInterval = 0.15f;
                    data.hasRearWeakSpot = false;
                    data.flankerTactic = FlankerTactic.StrafingRun;
                    break;

                case EnemyArchetype.Interceptor:
                    data.displayName = "Interceptor Chaser (Enemy 2)";
                    data.maxHealth = 35; // 2 hits nếu bắn diện, 1 hit nếu trúng đuôi (35 < 40)
                    data.scoreValue = 400;
                    data.threatCost = 35;
                    data.movementSpeed = 25f;
                    data.turnSpeed = 1.2f;
                    data.fireCooldown = 1.0f;
                    data.projectileSpeed = 75f; // Đạn Dart bay nhanh
                    data.burstCount = 1;
                    data.hasRearWeakSpot = true; // Điểm yếu sau đuôi
                    data.weakSpotMultiplier = 2.0f;
                    break;

                case EnemyArchetype.SquadLeader:
                    data.displayName = "Squad Leader (Heavy)";
                    data.maxHealth = 80; // 4 hits hoặc 1 Charge Shot
                    data.scoreValue = 800;
                    data.threatCost = 60;
                    data.movementSpeed = 20f;
                    data.turnSpeed = 0.6f;
                    data.fireCooldown = 1.5f;
                    data.projectileSpeed = 25f;
                    data.burstCount = 3; // Bắn chùm 3 viên
                    data.burstInterval = 0.2f;
                    data.hasRearWeakSpot = false;
                    break;

                case EnemyArchetype.ShieldBearer:
                    data.displayName = "Shield Escort (Defense Variant)";
                    data.maxHealth = 40; // 2 hits nếu mất khiên
                    data.scoreValue = 350;
                    data.threatCost = 30;
                    data.movementSpeed = 22f;
                    data.turnSpeed = 0.8f;
                    data.fireCooldown = 1.4f;
                    data.projectileSpeed = 25f;
                    data.burstCount = 1;
                    data.hasRearWeakSpot = false;
                    data.hasFormationShield = true;
                    break;
            }

            return data;
        }
    }
}
