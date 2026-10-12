using UnityEngine;
using UnityEditor;
using VirtualRail;

namespace VirtualRail.EditorTools
{
    /// <summary>
    /// Cong cu Editor 1-click quan ly va khoi tao Boss Federation Corvette F3 (Crimson Titan).
    /// Tuan thu nghiem ngat Quy tac An toan 3 Tang (AGENTS.md / GEMINI.md Dieu 3):
    /// - Tang 2: Tao va quan ly Prefab doc lap Assets/Prefabs/Boss_F3_Corvette.prefab.
    /// - Tang 3: Cung cap cap Menu Item 1-click doi xung: Spawn Test va Remove Test.
    /// </summary>
    [InitializeOnLoad]
    public static class VirtualRailBossEditorTool
    {
        public const string BOSS_PREFAB_PATH = "Assets/Prefabs/Boss_F3_Corvette.prefab";
        public const string F3_RED_FBX_PATH = "Assets/F3_Corvette/Meshes/F3_Red.FBX";
        public const string F3_RED_MAT_PATH = "Assets/F3_Corvette/Meshes/Materials/F3_Red.mat";
        public const string F3_RED_TEX_PATH = "Assets/F3_Corvette/Textures/F3_Red.tif";
        public const string F3_PURPLE_TEX_PATH = "Assets/F3_Corvette/Textures/F3_Purple.png";
        public const string SHIELD_MAT_PATH = "Assets/Materials/HexShield_ElectricCyan.mat";
        public const string EXPLOSION_VFX_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX3_Fire_Explosion.prefab";
        public const string SPARKS_VFX_PATH = "Assets/Asset Store/JMO Assets/Cartoon FX/Prefabs/CFX3_Hit_Electric_C_Air.prefab";

        static VirtualRailBossEditorTool()
        {
            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFAB_PATH))
                {
                    BuildOrUpdateBossPrefab();
                }
            };
        }

        [MenuItem("Tools/Virtual Rail/Boss/[1] Build or Update Boss F3 Prefab", false, 100)]
        public static GameObject BuildOrUpdateBossPrefab()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(F3_RED_FBX_PATH);
            if (fbx == null)
            {
                Debug.LogError($"[BossBuilder] Khong tim thay FBX tai: {F3_RED_FBX_PATH}");
                return null;
            }

            // Khoi tao doi tuong tam de thiet lap Component
            GameObject bossObj = Object.Instantiate(fbx);
            bossObj.name = "Boss_F3_Corvette";
            bossObj.tag = "Enemy";
            bossObj.transform.localScale = Vector3.one * 1.55f;

            GameObject expVfx = AssetDatabase.LoadAssetAtPath<GameObject>(EXPLOSION_VFX_PATH);
            GameObject sparkVfx = AssetDatabase.LoadAssetAtPath<GameObject>(SPARKS_VFX_PATH);

            // 1. Gan Controller trung tam
            var controller = bossObj.GetComponent<BossF3CorvetteController>();
            if (controller == null) controller = bossObj.AddComponent<BossF3CorvetteController>();

            // 2. Thiet lap Than Chinh [Main_Armored_Hull]
            Transform hullTrans = bossObj.transform.Find("[Main_Armored_Hull]");
            GameObject hullObj = hullTrans != null ? hullTrans.gameObject : new GameObject("[Main_Armored_Hull]");
            if (hullTrans == null)
            {
                hullObj.transform.SetParent(bossObj.transform, false);
                hullObj.transform.localPosition = Vector3.zero;
            }
            hullObj.tag = "Enemy";

            var hullCol = hullObj.GetComponent<BoxCollider>();
            if (hullCol == null) hullCol = hullObj.AddComponent<BoxCollider>();
            hullCol.size = new Vector3(30f, 10f, 108f);
            hullCol.center = new Vector3(0f, 1.5f, 0f);

            var hullHp = hullObj.GetComponent<BossHardpoint>();
            if (hullHp == null) hullHp = hullObj.AddComponent<BossHardpoint>();
            SetHardpointFields(hullHp, BossHardpointType.ArmoredHull, 1000, false, 1.0f);

            // 3. Thiet lap Thap phao Canh Trai [Turret_Left]
            Transform tLeftTrans = bossObj.transform.Find("[Turret_Left]");
            GameObject tLeftObj = tLeftTrans != null ? tLeftTrans.gameObject : new GameObject("[Turret_Left]");
            if (tLeftTrans == null)
            {
                tLeftObj.transform.SetParent(bossObj.transform, false);
                tLeftObj.transform.localPosition = new Vector3(-25f, 2f, 5f);
            }
            tLeftObj.tag = "Enemy";

            var tLeftCol = tLeftObj.GetComponent<BoxCollider>();
            if (tLeftCol == null) tLeftCol = tLeftObj.AddComponent<BoxCollider>();
            tLeftCol.size = new Vector3(16f, 10f, 40f);
            tLeftCol.center = Vector3.zero;

            var tLeftHp = tLeftObj.GetComponent<BossHardpoint>();
            if (tLeftHp == null) tLeftHp = tLeftObj.AddComponent<BossHardpoint>();
            SetHardpointFields(tLeftHp, BossHardpointType.LeftFlakTurret, 150, false, 1.0f);

            // Muzzles trai
            Transform mLeftTrans = tLeftObj.transform.Find("Muzzle_L1");
            if (mLeftTrans == null)
            {
                var mObj = new GameObject("Muzzle_L1");
                mObj.transform.SetParent(tLeftObj.transform, false);
                mObj.transform.localPosition = new Vector3(0f, 4f, 0f);
            }

            // 4. Thiet lap Thap phao Canh Phai [Turret_Right]
            Transform tRightTrans = bossObj.transform.Find("[Turret_Right]");
            GameObject tRightObj = tRightTrans != null ? tRightTrans.gameObject : new GameObject("[Turret_Right]");
            if (tRightTrans == null)
            {
                tRightObj.transform.SetParent(bossObj.transform, false);
                tRightObj.transform.localPosition = new Vector3(25f, 2f, 5f);
            }
            tRightObj.tag = "Enemy";

            var tRightCol = tRightObj.GetComponent<BoxCollider>();
            if (tRightCol == null) tRightCol = tRightObj.AddComponent<BoxCollider>();
            tRightCol.size = new Vector3(16f, 10f, 40f);
            tRightCol.center = Vector3.zero;

            var tRightHp = tRightObj.GetComponent<BossHardpoint>();
            if (tRightHp == null) tRightHp = tRightObj.AddComponent<BossHardpoint>();
            SetHardpointFields(tRightHp, BossHardpointType.RightFlakTurret, 150, false, 1.0f);

            // Muzzles phai
            Transform mRightTrans = tRightObj.transform.Find("Muzzle_R1");
            if (mRightTrans == null)
            {
                var mObj = new GameObject("Muzzle_R1");
                mObj.transform.SetParent(tRightObj.transform, false);
                mObj.transform.localPosition = new Vector3(0f, 4f, 0f);
            }

            // 5. Thiet lap Ranh Lo Phan Ung [Dorsal_Reactor_Trench] (Diem yeu)
            Transform trenchTrans = bossObj.transform.Find("[Dorsal_Reactor_Trench]");
            GameObject trenchObj = trenchTrans != null ? trenchTrans.gameObject : new GameObject("[Dorsal_Reactor_Trench]");
            if (trenchTrans == null)
            {
                trenchObj.transform.SetParent(bossObj.transform, false);
                trenchObj.transform.localPosition = new Vector3(0f, 6.5f, 5f);
            }
            trenchObj.tag = "Enemy";

            var trenchCol = trenchObj.GetComponent<BoxCollider>();
            if (trenchCol == null) trenchCol = trenchObj.AddComponent<BoxCollider>();
            trenchCol.size = new Vector3(8f, 5f, 50f);
            trenchCol.center = Vector3.zero;

            var trenchHp = trenchObj.GetComponent<BossHardpoint>();
            if (trenchHp == null) trenchHp = trenchObj.AddComponent<BossHardpoint>();
            SetHardpointFields(trenchHp, BossHardpointType.DorsalReactorTrench, 300, true, 2.0f);

            // Muzzle cho Ranh Lo Phan Ung
            Transform ventTrans = trenchObj.transform.Find("Muzzle_Vent");
            if (ventTrans == null)
            {
                var ventObj = new GameObject("Muzzle_Vent");
                ventObj.transform.SetParent(trenchObj.transform, false);
                ventObj.transform.localPosition = new Vector3(0f, 3f, 0f);
                ventTrans = ventObj.transform;
            }

            // Loi nang luong Truc quan [Reactor_Core_Visual]
            Transform visualTrans = trenchObj.transform.Find("[Reactor_Core_Visual]");
            GameObject visualObj = visualTrans != null ? visualTrans.gameObject : null;
            if (visualObj == null)
            {
                visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObj.name = "[Reactor_Core_Visual]";
                visualObj.transform.SetParent(trenchObj.transform, false);
                visualObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = new Vector3(4.5f, 1.2f, 48f);

                // Xoa Collider du thua tren visual primitive de tranh xung dot raycast
                var primCol = visualObj.GetComponent<Collider>();
                if (primCol != null) Object.DestroyImmediate(primCol);
            }

            Material plasmaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ReactorTrench_HotPlasma.mat");
            var coreRenderer = visualObj.GetComponent<Renderer>();
            if (coreRenderer != null && plasmaMat != null)
            {
                coreRenderer.sharedMaterial = plasmaMat;
            }

            var reactorVisual = trenchObj.GetComponent<VirtualRailReactorTrenchVisual>();
            if (reactorVisual == null) reactorVisual = trenchObj.AddComponent<VirtualRailReactorTrenchVisual>();

            SerializedObject rso = new SerializedObject(reactorVisual);
            rso.FindProperty("_coreRenderer").objectReferenceValue = coreRenderer;
            rso.FindProperty("_ventMuzzle").objectReferenceValue = ventTrans;
            if (sparkVfx != null) rso.FindProperty("_chargeSparksPrefab").objectReferenceValue = sparkVfx;
            rso.ApplyModifiedProperties();

            // 6. Thiet lap Cau Tau Chi Huy [Main_Bridge]
            Transform bridgeTrans = bossObj.transform.Find("[Main_Bridge]");
            GameObject bridgeObj = bridgeTrans != null ? bridgeTrans.gameObject : new GameObject("[Main_Bridge]");
            if (bridgeTrans == null)
            {
                bridgeObj.transform.SetParent(bossObj.transform, false);
                bridgeObj.transform.localPosition = new Vector3(0f, 9f, -12f);
            }
            bridgeObj.tag = "Enemy";

            var bridgeCol = bridgeObj.GetComponent<BoxCollider>();
            if (bridgeCol == null) bridgeCol = bridgeObj.AddComponent<BoxCollider>();
            bridgeCol.size = new Vector3(14f, 8f, 18f);
            bridgeCol.center = Vector3.zero;

            var bridgeHp = bridgeObj.GetComponent<BossHardpoint>();
            if (bridgeHp == null) bridgeHp = bridgeObj.AddComponent<BossHardpoint>();
            SetHardpointFields(bridgeHp, BossHardpointType.MainBridge, 200, false, 1.0f);

            // 7. Thiet lap Khien Hex-Shield [Shield_Barrier] (Dorsal Deck Shield Fit)
            Transform shieldTrans = bossObj.transform.Find("[Shield_Barrier]");
            GameObject shieldObj = shieldTrans != null ? shieldTrans.gameObject : new GameObject("[Shield_Barrier]");
            if (shieldTrans == null)
            {
                shieldObj.transform.SetParent(bossObj.transform, false);
            }
            shieldObj.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            shieldObj.transform.localScale = new Vector3(36f, 10.5f, 114f);
            shieldObj.tag = "Enemy";

            var hexShield = shieldObj.GetComponent<VirtualRailHexShield>();
            if (hexShield == null) hexShield = shieldObj.AddComponent<VirtualRailHexShield>();

            var shieldCol = shieldObj.GetComponent<BoxCollider>();
            if (shieldCol == null) shieldCol = shieldObj.AddComponent<BoxCollider>();
            shieldCol.size = Vector3.one;

            var shieldRenderer = shieldObj.GetComponent<MeshRenderer>();
            if (shieldRenderer == null) shieldRenderer = shieldObj.AddComponent<MeshRenderer>();

            var shieldFilter = shieldObj.GetComponent<MeshFilter>();
            if (shieldFilter == null) shieldFilter = shieldObj.AddComponent<MeshFilter>();
            if (shieldFilter.sharedMesh == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shieldFilter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(cube);
            }

            Material shieldMat = AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MAT_PATH);
            if (shieldMat != null) shieldRenderer.sharedMaterial = shieldMat;

            // 8. Gan cac tham chieu VFX vao Controller
            SerializedObject soShield = new SerializedObject(hexShield);
            soShield.FindProperty("_maxShieldHealth").intValue = 300;
            soShield.FindProperty("_currentShieldHealth").intValue = 300;
            if (sparkVfx != null) soShield.FindProperty("_overloadVfxPrefab").objectReferenceValue = sparkVfx;
            soShield.ApplyModifiedProperties();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_maxHealth").intValue = 1000;
            so.FindProperty("_leadDistanceZ").floatValue = 68f;
            so.FindProperty("_baseElevationY").floatValue = -22f;
            so.FindProperty("_basePitchAngle").floatValue = 7.5f;
            so.FindProperty("_horizontalWaveAmp").floatValue = 6.5f;
            so.FindProperty("_horizontalWaveFreq").floatValue = 0.45f;
            so.FindProperty("_introStartElevationY").floatValue = -65f;

            so.FindProperty("_turretLeft").objectReferenceValue = tLeftHp;
            so.FindProperty("_turretRight").objectReferenceValue = tRightHp;
            so.FindProperty("_reactorTrench").objectReferenceValue = trenchHp;
            so.FindProperty("_mainBridge").objectReferenceValue = bridgeHp;
            so.FindProperty("_hexShield").objectReferenceValue = hexShield;
            so.FindProperty("_reactorVentMuzzle").objectReferenceValue = ventTrans;
            so.FindProperty("_reactorVisual").objectReferenceValue = reactorVisual;

            if (expVfx != null) so.FindProperty("_explosionPrefab").objectReferenceValue = expVfx;
            if (sparkVfx != null) so.FindProperty("_stunSparksPrefab").objectReferenceValue = sparkVfx;

            SerializedProperty muzzlePropL = so.FindProperty("_flakMuzzlesLeft");
            muzzlePropL.arraySize = 1;
            muzzlePropL.GetArrayElementAtIndex(0).objectReferenceValue = tLeftObj.transform.Find("Muzzle_L1");

            SerializedProperty muzzlePropR = so.FindProperty("_flakMuzzlesRight");
            muzzlePropR.arraySize = 1;
            muzzlePropR.GetArrayElementAtIndex(0).objectReferenceValue = tRightObj.transform.Find("Muzzle_R1");

            SerializedProperty vlsProp = so.FindProperty("_vlsMissileSilos");
            vlsProp.arraySize = 2;
            vlsProp.GetArrayElementAtIndex(0).objectReferenceValue = tLeftObj.transform;
            vlsProp.GetArrayElementAtIndex(1).objectReferenceValue = tRightObj.transform;

            so.ApplyModifiedProperties();

            // Luu thanh Prefab doc lap moi
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bossObj, BOSS_PREFAB_PATH);
            Object.DestroyImmediate(bossObj);

            Debug.Log($"<color=lime><b>[BossBuilder] ĐÃ TẠO THÀNH CÔNG PREFAB: {BOSS_PREFAB_PATH}</b></color>");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return prefab;
        }

        private static void SetHardpointFields(BossHardpoint hp, BossHardpointType type, int maxHealth, bool weakSpot, float weakMult)
        {
            SerializedObject so = new SerializedObject(hp);
            so.FindProperty("_hardpointType").enumValueIndex = (int)type;
            so.FindProperty("_maxHealth").intValue = maxHealth;
            so.FindProperty("_isWeakSpot").boolValue = weakSpot;
            so.FindProperty("_weakSpotDamageMultiplier").floatValue = weakMult;
            so.ApplyModifiedProperties();
        }

        [MenuItem("Tools/Virtual Rail/Boss/[2] Spawn Boss F3 Test (Scene)", false, 101)]
        public static void SpawnBossTestInScene()
        {
            RemoveBossTestFromScene();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFAB_PATH);
            if (prefab == null)
            {
                prefab = BuildOrUpdateBossPrefab();
            }

            if (prefab == null) return;

            VirtualRailAnchor anchor = Object.FindAnyObjectByType<VirtualRailAnchor>();
            Vector3 spawnPos = (anchor != null) 
                ? anchor.transform.position + anchor.ForwardTangent * 68f - anchor.transform.up * 22f 
                : new Vector3(0f, -22f, 68f);

            Quaternion spawnRot = (anchor != null) 
                ? Quaternion.LookRotation(anchor.ForwardTangent, anchor.transform.up) * Quaternion.Euler(-7.5f, 0f, 0f)
                : Quaternion.Euler(-7.5f, 0f, 0f);

            GameObject testBoss = Object.Instantiate(prefab, spawnPos, spawnRot);
            testBoss.name = "[Active_Boss_F3_Corvette_Test]";
            testBoss.transform.localScale = Vector3.one * 1.55f;
            Selection.activeGameObject = testBoss;

            Debug.Log("<color=cyan><b>[BossTool] Đã triệu hồi Boss F3 Test trực tiếp dưới bụng tàu Arwing trong Scene!</b></color>");
        }

        [MenuItem("Tools/Virtual Rail/Boss/[3] Remove Boss F3 Test (Scene)", false, 102)]
        public static void RemoveBossTestFromScene()
        {
            var existingTest = GameObject.Find("[Active_Boss_F3_Corvette_Test]");
            if (existingTest != null)
            {
                Object.DestroyImmediate(existingTest);
            }

            var bosses = Object.FindObjectsByType<BossF3CorvetteController>(FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] != null)
                {
                    Object.DestroyImmediate(bosses[i].gameObject);
                }
            }

            Debug.Log("<color=yellow><b>[BossTool] Đã thu dọn sạch sẽ toàn bộ Boss Test khỏi Scene!</b></color>");
        }

        [MenuItem("Tools/Virtual Rail/Boss/[4] Attach Boss Bar To Modern HUD (Scene)", false, 103)]
        public static void AttachBossBarToSceneHUD()
        {
            var hud = Object.FindAnyObjectByType<ModernCockpitHUD>();
            if (hud == null)
            {
                Debug.LogWarning("[BossTool] Không tìm thấy ModernCockpitHUD trong Scene!");
                return;
            }

            Transform existing = hud.transform.Find("Cluster_Boss");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Color bgPanelColor = new Color(0.02f, 0.08f, 0.14f, 0.88f);
            Color amberColor = new Color(1f, 0.68f, 0.1f, 1f);
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/HUD/hud_frame_bracket.png");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject bossCluster = new GameObject("Cluster_Boss");
            bossCluster.transform.SetParent(hud.transform, false);

            RectTransform rt = bossCluster.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -22f);
            rt.sizeDelta = new Vector2(540f, 74f);

            UnityEngine.UI.Image panelBg = bossCluster.AddComponent<UnityEngine.UI.Image>();
            if (frameSprite != null)
            {
                panelBg.sprite = frameSprite;
                panelBg.type = UnityEngine.UI.Image.Type.Sliced;
                panelBg.color = Color.white;
            }
            else
            {
                panelBg.color = bgPanelColor;
            }

            // Boss Name Text
            GameObject nameObj = new GameObject("BossName");
            nameObj.transform.SetParent(bossCluster.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            nameRt.sizeDelta = new Vector2(-24f, 16f);

            var nameTxt = nameObj.AddComponent<UnityEngine.UI.Text>();
            nameTxt.font = font;
            nameTxt.fontSize = 12;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = amberColor;
            nameTxt.text = "FEDERATION CORVETTE F3 // CRIMSON TITAN";

            // 1. Boss Shield Bar (Layer 1 - Cyan)
            GameObject sBarObj = new GameObject("ShieldBar");
            sBarObj.transform.SetParent(bossCluster.transform, false);
            RectTransform sBarRt = sBarObj.AddComponent<RectTransform>();
            sBarRt.anchorMin = new Vector2(0f, 0.5f);
            sBarRt.anchorMax = new Vector2(1f, 0.5f);
            sBarRt.pivot = new Vector2(0.5f, 0.5f);
            sBarRt.anchoredPosition = new Vector2(0f, 6f);
            sBarRt.sizeDelta = new Vector2(-28f, 9f);

            UnityEngine.UI.Image sTrackImg = sBarObj.AddComponent<UnityEngine.UI.Image>();
            sTrackImg.color = new Color(0.04f, 0.10f, 0.16f, 0.95f);

            GameObject sGhostObj = new GameObject("GhostFill");
            sGhostObj.transform.SetParent(sBarObj.transform, false);
            RectTransform sGhostRt = sGhostObj.AddComponent<RectTransform>();
            sGhostRt.anchorMin = Vector2.zero;
            sGhostRt.anchorMax = Vector2.one;
            sGhostRt.offsetMin = Vector2.zero;
            sGhostRt.offsetMax = Vector2.zero;

            var sGhostImg = sGhostObj.AddComponent<UnityEngine.UI.Image>();
            sGhostImg.type = UnityEngine.UI.Image.Type.Filled;
            sGhostImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            sGhostImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            sGhostImg.fillAmount = 1f;
            sGhostImg.color = new Color(0.2f, 0.85f, 1f, 0.55f);

            GameObject sFillObj = new GameObject("Fill");
            sFillObj.transform.SetParent(sBarObj.transform, false);
            RectTransform sFillRt = sFillObj.AddComponent<RectTransform>();
            sFillRt.anchorMin = Vector2.zero;
            sFillRt.anchorMax = Vector2.one;
            sFillRt.offsetMin = Vector2.zero;
            sFillRt.offsetMax = Vector2.zero;

            var sFillImg = sFillObj.AddComponent<UnityEngine.UI.Image>();
            sFillImg.type = UnityEngine.UI.Image.Type.Filled;
            sFillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            sFillImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            sFillImg.fillAmount = 1f;
            sFillImg.color = new Color(0f, 0.95f, 1f, 1f); // Neon Cyan

            // 2. Boss Health Bar (Layer 2 - Red)
            GameObject barObj = new GameObject("HealthBar");
            barObj.transform.SetParent(bossCluster.transform, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.5f);
            barRt.anchorMax = new Vector2(1f, 0.5f);
            barRt.pivot = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = new Vector2(0f, -7f);
            barRt.sizeDelta = new Vector2(-28f, 11f);

            UnityEngine.UI.Image trackImg = barObj.AddComponent<UnityEngine.UI.Image>();
            trackImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            GameObject ghostObj = new GameObject("GhostFill");
            ghostObj.transform.SetParent(barObj.transform, false);
            RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
            ghostRt.anchorMin = Vector2.zero;
            ghostRt.anchorMax = Vector2.one;
            ghostRt.offsetMin = Vector2.zero;
            ghostRt.offsetMax = Vector2.zero;

            var ghostImg = ghostObj.AddComponent<UnityEngine.UI.Image>();
            ghostImg.type = UnityEngine.UI.Image.Type.Filled;
            ghostImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            ghostImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            ghostImg.fillAmount = 1f;
            ghostImg.color = new Color(1f, 0.85f, 0.35f, 0.7f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(1f, 0.22f, 0.15f, 1f);

            // Phase Text
            GameObject phaseObj = new GameObject("PhaseText");
            phaseObj.transform.SetParent(bossCluster.transform, false);
            RectTransform phaseRt = phaseObj.AddComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0f, 0f);
            phaseRt.anchorMax = new Vector2(1f, 0f);
            phaseRt.pivot = new Vector2(0.5f, 0f);
            phaseRt.anchoredPosition = new Vector2(0f, 4f);
            phaseRt.sizeDelta = new Vector2(-24f, 14f);

            var phaseTxt = phaseObj.AddComponent<UnityEngine.UI.Text>();
            phaseTxt.font = font;
            phaseTxt.fontSize = 10;
            phaseTxt.fontStyle = FontStyle.Normal;
            phaseTxt.alignment = TextAnchor.MiddleCenter;
            phaseTxt.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
            phaseTxt.text = "PHASE 1: DORSAL SHIELD ONLINE";

            // Wire up SerializedObject references
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("clusterBossRoot").objectReferenceValue = rt;
            so.FindProperty("bossNameText").objectReferenceValue = nameTxt;
            so.FindProperty("bossGhostBarFill").objectReferenceValue = ghostImg;
            so.FindProperty("bossHealthBarFill").objectReferenceValue = fillImg;
            so.FindProperty("bossShieldBarFill").objectReferenceValue = sFillImg;
            so.FindProperty("bossShieldGhostBarFill").objectReferenceValue = sGhostImg;
            so.FindProperty("bossPhaseText").objectReferenceValue = phaseTxt;
            so.ApplyModifiedProperties();

            bossCluster.SetActive(false);

            EditorUtility.SetDirty(hud.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(hud.gameObject.scene);

            Debug.Log("<color=lime><b>[BossTool] Đã gắn thành công Cụm 6 Boss Health Bar vào ModernCockpitHUD trong Scene và đã lưu!</b></color>");
        }

        #region BOSS HULL COLOR ACCENT SWITCHER (DARK PURPLE & CRIMSON RED)
        [MenuItem("Tools/Virtual Rail/Boss/[10] Switch Boss Hull Accent to Dark Purple (ON)", false, 110)]
        [MenuItem("Tools/Virtual Rail/Boss Corvette/1. Switch Hull Accent to Dark Purple (ON)")]
        public static void SwitchBossHullAccentToDarkPurple()
        {
            Texture2D purpleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(F3_PURPLE_TEX_PATH);
            if (purpleTex == null)
            {
                purpleTex = GenerateDarkPurpleTexture();
            }

            if (purpleTex == null)
            {
                Debug.LogError($"[BossEditorTool] Khong the tai hoac tao texture F3_Purple tai {F3_PURPLE_TEX_PATH}!");
                return;
            }

            Material bossMat = AssetDatabase.LoadAssetAtPath<Material>(F3_RED_MAT_PATH);
            if (bossMat != null)
            {
                bossMat.SetTexture("_MainTex", purpleTex);
                EditorUtility.SetDirty(bossMat);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=magenta><b>[BossEditorTool] ĐÃ CHUYỂN MÀU SƠN BOSS THÀNH TÍM THẪM (DARK PURPLE - ON):</b></color> Đã gán F3_Purple.png vào Material F3_Red.mat thành công!");
            }
        }

        [MenuItem("Tools/Virtual Rail/Boss/[11] Revert Boss Hull Accent to Crimson Red (OFF - Legacy)", false, 111)]
        [MenuItem("Tools/Virtual Rail/Boss Corvette/2. Revert Hull Accent to Crimson Red (OFF - Legacy)")]
        public static void RevertBossHullAccentToCrimsonRed()
        {
            Texture2D redTex = AssetDatabase.LoadAssetAtPath<Texture2D>(F3_RED_TEX_PATH);
            if (redTex == null)
            {
                Debug.LogError($"[BossEditorTool] Khong tim thay texture goc tai {F3_RED_TEX_PATH}!");
                return;
            }

            Material bossMat = AssetDatabase.LoadAssetAtPath<Material>(F3_RED_MAT_PATH);
            if (bossMat != null)
            {
                bossMat.SetTexture("_MainTex", redTex);
                EditorUtility.SetDirty(bossMat);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=yellow><b>[BossEditorTool] ĐÃ HOÀN NGUYÊN MÀU SƠN BOSS VỀ ĐỎ THẪM (CRIMSON RED - OFF):</b></color> Đã khôi phục F3_Red.tif gốc thành công!");
            }
        }

        private static Texture2D GenerateDarkPurpleTexture()
        {
            TextureImporter importer = AssetImporter.GetAtPath(F3_RED_TEX_PATH) as TextureImporter;
            if (importer == null) return null;
            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D src = AssetDatabase.LoadAssetAtPath<Texture2D>(F3_RED_TEX_PATH);
            if (src == null) return null;

            Color32[] pixels = src.GetPixels32();
            int len = pixels.Length;
            for (int i = 0; i < len; i++)
            {
                Color32 c = pixels[i];
                if (c.r > c.g + 5 && c.r > c.b + 5)
                {
                    float r = c.r / 255f;
                    float g = c.g / 255f;
                    float b = c.b / 255f;
                    float maxC = Mathf.Max(r, Mathf.Max(g, b));
                    float minC = Mathf.Min(r, Mathf.Min(g, b));
                    float delta = maxC - minC;
                    float s = maxC > 0.001f ? delta / maxC : 0f;

                    if (s >= 0.35f)
                    {
                        byte newB = (byte)Mathf.Clamp(Mathf.RoundToInt(maxC * 255f), 0, 255);
                        byte newG = (byte)Mathf.Clamp(Mathf.RoundToInt(minC * 255f), 0, 255);
                        byte newR = (byte)Mathf.Clamp(Mathf.RoundToInt((minC + (2f / 3f) * delta) * 255f), 0, 255);
                        pixels[i] = new Color32(newR, newG, newB, c.a);
                    }
                }
            }

            Texture2D purple = new Texture2D(src.width, src.height, TextureFormat.RGB24, true);
            purple.SetPixels32(pixels);
            purple.Apply();

            byte[] pngData = purple.EncodeToPNG();
            System.IO.File.WriteAllBytes(F3_PURPLE_TEX_PATH, pngData);
            AssetDatabase.ImportAsset(F3_PURPLE_TEX_PATH, ImportAssetOptions.ForceUpdate);

            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(F3_PURPLE_TEX_PATH);
        }
        #endregion
    }
}
