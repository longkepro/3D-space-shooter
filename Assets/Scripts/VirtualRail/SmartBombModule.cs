using System;
using UnityEngine;

namespace VirtualRail
{
    /// <summary>
    /// Module Bom Thông Minh (Smart Bomb Screen-Clear) theo chuẩn TDD v1.0.0 Phần I.5.
    /// Kích hoạt sóng xung kích 150m:
    /// - Quét sạch 100% đạn địch trong khung nhìn.
    /// - Gây sát thương diện rộng cực lớn lên toàn bộ kẻ địch và thiên thạch.
    /// </summary>
    public class SmartBombModule : MonoBehaviour
    {
        public static SmartBombModule Instance { get; private set; }

        public static event Action<int> OnBombCountChanged;

        [Header("Settings")]
        [SerializeField] private int initialBombs = 3;
        [SerializeField] private float shockwaveRadius = 150f;
        [SerializeField] private KeyCode bombKey = KeyCode.B;

        private int _currentBombs;
        private VirtualRailAnchor _anchor;

        public int CurrentBombs => _currentBombs;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _anchor = GetComponentInParent<VirtualRailAnchor>();
            if (_anchor == null) _anchor = VirtualRailAnchor.Instance;

            if (_anchor != null && _anchor.config != null)
            {
                initialBombs = _anchor.config.initialSmartBombs;
            }
            _currentBombs = initialBombs;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            OnBombCountChanged?.Invoke(_currentBombs);
        }

        private void Update()
        {
            if (_anchor != null && _anchor.config != null && !_anchor.config.enableSmartBomb)
            {
                return;
            }

            if (Input.GetKeyDown(bombKey))
            {
                TriggerBomb();
            }
        }

        public bool TriggerBomb()
        {
            if (_currentBombs <= 0)
            {
                Debug.LogWarning("[SmartBomb] Đã hết bom dự trữ!");
                return false;
            }

            _currentBombs--;
            OnBombCountChanged?.Invoke(_currentBombs);

            Vector3 centerPos = transform.position;

            // 1. Quét sạch 100% đạn địch đang tồn tại
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.ClearAllActiveProjectiles();
            }

            // 2. Phá hủy toàn bộ quái vật và thiên thạch trong bán kính 150m
            Collider[] colliders = Physics.OverlapSphere(centerPos, shockwaveRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;

                var enemy = colliders[i].GetComponentInParent<EnemyMovement>();
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    enemy.BlowUp();
                    continue;
                }

                var asteroid = colliders[i].GetComponentInParent<Asteroid>();
                if (asteroid != null && asteroid.gameObject.activeSelf)
                {
                    var exp = asteroid.GetComponent<Explosion>();
                    if (exp != null) exp.BlowUp();
                    else asteroid.Recycle();
                }
            }

            Debug.Log($"<color=cyan><b>[SMART BOMB]</b></color> Đã kích hoạt sóng xung kích 150m! Còn lại {_currentBombs} quả.");
            return true;
        }

        public void AddBomb(int count = 1)
        {
            _currentBombs += count;
            OnBombCountChanged?.Invoke(_currentBombs);
        }
    }
}
