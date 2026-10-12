using UnityEngine;
using UnityEngine.UI;

namespace VirtualRail
{
    /// <summary>
    /// Hiển thị chỉ số FPS theo thời gian thực ở góc màn hình.
    /// Tuân thủ 0 GC Allocation trong Update bằng bảng chuỗi tính sẵn.
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        public Text fpsText;

        private float _deltaTime = 0.0f;
        private float _updateInterval = 0.25f;
        private float _timeSinceLastUpdate = 0.0f;
        private int _lastFPS = -1;

        private static readonly string[] FPS_STRINGS = PrecomputeFPSStrings();

        private static string[] PrecomputeFPSStrings()
        {
            string[] table = new string[301];
            for (int i = 0; i <= 300; i++)
            {
                table[i] = "FPS: " + i;
            }
            return table;
        }

        private void Awake()
        {
            if (fpsText == null) fpsText = GetComponent<Text>();
        }

        private void Update()
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.showFPS)
            {
                if (fpsText != null && fpsText.enabled) fpsText.enabled = false;
                return;
            }

            if (fpsText != null && !fpsText.enabled) fpsText.enabled = true;

            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _timeSinceLastUpdate += Time.unscaledDeltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                _timeSinceLastUpdate = 0f;
                int currentFPS = Mathf.Clamp(Mathf.RoundToInt(1.0f / Mathf.Max(0.0001f, _deltaTime)), 0, 300);

                if (currentFPS != _lastFPS)
                {
                    _lastFPS = currentFPS;
                    if (fpsText != null)
                    {
                        fpsText.text = FPS_STRINGS[currentFPS];
                    }
                }
            }
        }
    }
}
