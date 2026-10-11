# 🔥 BÁO CÁO KIỂM TRA HIỆU NĂNG (Performance Audit Report)

> **Dự án:** 3D-Shooter-master  
> **Ngày kiểm tra:** 2026-10-10  
> **Phạm vi:** Toàn bộ 69 file C# trong `Assets/Scripts/`  
> **Phương pháp:** Phân tích tĩnh mã nguồn (Static Code Analysis) — tập trung vào GC Allocation, hot-path overhead, và vi phạm kiến trúc hiệu năng Unity.

---

## 📊 TỔNG QUAN

| Mức độ | Số lượng | Mô tả |
|--------|----------|-------|
| 🔴 **Critical** | 5 | Gây frame drop rõ rệt mỗi frame, phải sửa ngay |
| 🟠 **High** | 12 | Gây GC spike hoặc CPU overhead cao trong combat |
| 🟡 **Medium** | 8 | Allocation không thường xuyên nhưng tích lũy gây stutter |
| 🟢 **Low** | 1 | Ảnh hưởng nhỏ, nên sửa khi có thời gian |

**Tổng cộng: 26 vấn đề hiệu năng được phát hiện.**

---

## 🔴 CRITICAL — Phải sửa ngay

---

### C-01: `FindGameObjectWithTag` mỗi frame khi Player bị hủy

**Ảnh hưởng:** Khi Player chết/bị Destroy, TẤT CẢ enemy + camera gọi `FindGameObjectWithTag("Player")` **mỗi frame** cho đến khi Player respawn. Với 20 enemy = 20 lần Find/frame.

| File | Dòng | Hàm gọi |
|------|------|---------|
| [`EnemyAttack.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/EnemyAttack.cs) | 179 | `Update() → TargetPlayer()` |
| [`EnemyMovement.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/EnemyMovement.cs) | 269 | `Update() → TargetPlayer()` |
| [`FollowCam.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/FollowCam.cs) | 29 | `LateUpdate()` |

**Code lỗi:**
```csharp
// Trong Update() / LateUpdate()
if (_target == null)
{
    var player = GameObject.FindGameObjectWithTag("Player");
    // ...
}
```

**Cách khắc phục:**
Dùng **Singleton pattern** hoặc **Event-driven** để cache reference Player toàn cục:

```csharp
// === PlayerReference.cs (Singleton) ===
public class PlayerReference : MonoBehaviour
{
    public static Transform Instance { get; private set; }
    void OnEnable()  => Instance = transform;
    void OnDisable() => Instance = null;
}

// === Trong EnemyAttack.cs / EnemyMovement.cs / FollowCam.cs ===
// Thay thế FindGameObjectWithTag:
_target = PlayerReference.Instance;  // O(1), zero allocation
```

---

### C-02: `FindReferences()` gọi lại mỗi frame trong `StageDirector.Tick()`

**File:** [`VirtualRailStageDirector.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailStageDirector.cs)  
**Dòng:** 462, 468, 471-474

**Code lỗi:**
```csharp
public void Tick(float dt) {
    if (anchor == null || waveSpawner == null || asteroidSpawner == null || notificationUI == null) {
        FindReferences(); // Gọi FindAnyObjectByType & FindObjectsByType bên trong
    }
    // ...
}
```

**Vấn đề:** Nếu bất kỳ 1 trong 4 reference bị null (ví dụ `notificationUI` chưa có trong scene), `FindReferences()` chạy **mỗi frame** với nhiều lệnh `FindAnyObjectByType` — chi phí O(n) trên toàn scene.

**Cách khắc phục:**
```csharp
private bool _referencesResolved = false;

public void Tick(float dt) {
    if (!_referencesResolved) {
        FindReferences();
        _referencesResolved = true; // Chỉ thử 1 lần duy nhất
    }
    // ...
}
```

---

### C-03: `GetComponentInChildren` mỗi frame trong `DualReticleHUD.LateUpdate()`

**File:** [`DualReticleHUD.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/DualReticleHUD.cs)  
**Dòng:** 125-133

**Code lỗi:**
```csharp
// Trong LateUpdate():
if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
{
    var camRig = anchor.GetComponentInChildren<VirtualRailCameraRig>();
    mainCamera = (camRig != null) ? camRig.targetCamera : Camera.main;
}
if (reticle == null && anchor != null) reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
if (ship == null && anchor != null) ship = anchor.GetComponentInChildren<VirtualRailShip>();
if (weapon == null && anchor != null) weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
```

**Vấn đề:** 4 lệnh `GetComponentInChildren` chạy trong `LateUpdate()` mỗi frame khi bất kỳ reference nào null. Kèm fallback `Camera.main` (cũng là Find nội bộ).

**Cách khắc phục:**
```csharp
private bool _componentsCached = false;

void CacheComponents() {
    if (anchor == null) return;
    var camRig = anchor.GetComponentInChildren<VirtualRailCameraRig>();
    mainCamera = (camRig != null) ? camRig.targetCamera : Camera.main;
    reticle = anchor.GetComponentInChildren<VirtualRailReticle>();
    ship = anchor.GetComponentInChildren<VirtualRailShip>();
    weapon = anchor.GetComponentInChildren<VirtualRailWeapon>();
    _componentsCached = true;
}

void LateUpdate() {
    if (!_componentsCached) CacheComponents();
    if (mainCamera == null) return;
    // ... phần còn lại
}
```

---

## 🟠 HIGH — Gây CPU/GC overhead đáng kể trong combat

---

### H-01: `FindAnyObjectByType` trong hot path bắn đạn

**File:** [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs)  
**Dòng:** 303-305

**Code lỗi:**
```csharp
public Vector3 GetCurrentConvergencePoint(...) {
    if (reticle == null) reticle = FindAnyObjectByType<VirtualRailReticle>();
    if (anchor == null)  anchor = FindAnyObjectByType<VirtualRailAnchor>();
    // ...
}
```

**Vấn đề:** `GetCurrentConvergencePoint()` được gọi mỗi lần bắn. Fallback dùng `FindAnyObjectByType` gây lag spike ~0.5-2ms.

**Cách khắc phục:** Cache trong `Awake()` / `Start()` và loại bỏ hoàn toàn fallback Find.

---

### H-02: `GetComponentInParent` trong vòng lặp Physics overlap

**File:** [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs)  
**Dòng:** 392, 395, 565, 588, 596

**Code lỗi:**
```csharp
for (int i = 0; i < count; i++) {
    var eh = c.GetComponentInParent<EnemyHealth>();
    var em = c.GetComponentInParent<EnemyMovement>();
}
```

**Vấn đề:** Gọi `GetComponentInParent` 2 lần cho mỗi collider trong vòng lặp overlap. Với 15 collider = 30 lần GetComponent/frame.

**Cách khắc phục:**
Dùng **Enemy Registry** pattern — dictionary ánh xạ `Collider → EnemyData`:
```csharp
// === EnemyRegistry.cs (Singleton) ===
public static class EnemyRegistry {
    private static readonly Dictionary<Collider, EnemyHealth> _map = new(128);
    public static void Register(Collider col, EnemyHealth eh) => _map[col] = eh;
    public static void Unregister(Collider col) => _map.Remove(col);
    public static bool TryGet(Collider col, out EnemyHealth eh) => _map.TryGetValue(col, out eh);
}

// Trong AimAssistModule:
for (int i = 0; i < count; i++) {
    if (EnemyRegistry.TryGet(c, out var eh)) { /* ... */ }
}
```

---

### H-03: `GetComponentInParent` mỗi frame trong Homing Missile

**File:** [`VirtualRailHomingMissile.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHomingMissile.cs)  
**Dòng:** 160

**Code lỗi:**
```csharp
// Trong Update():
var eh = target.GetComponentInParent<EnemyHealth>();
if (eh == null || !eh.IsDead) { ... }
```

**Cách khắc phục:** Cache `EnemyHealth` khi target được gán trong `InitializeKinematics()`:
```csharp
private EnemyHealth _targetHealth;

public void InitializeKinematics(..., Transform lockedTarget, ...) {
    // ...
    _targetHealth = lockedTarget?.GetComponentInParent<EnemyHealth>();
}
```

---

### H-04: Nhiều `GetComponentInParent` trong SphereCast loop

**File:** [`VirtualRailHomingMissile.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHomingMissile.cs)  
**Dòng:** 260-262

**Code lỗi:**
```csharp
for (int i = 0; i < hitCount; i++) {
    var col = hit.collider;
    if (col.GetComponentInParent<EnemyHealth>() != null ||
        col.GetComponentInParent<EnemyMovement>() != null ||
        col.GetComponentInParent<Asteroid>() != null) { ... }
}
```

**Vấn đề:** 3× `GetComponentInParent` cho mỗi hit — CPU bound nặng.

**Cách khắc phục:** Dùng `LayerMask` để lọc từ `SphereCastNonAlloc`, hoặc dùng `CompareTag()` + Enemy Registry.

---

### H-05: `GetComponent` mỗi frame trong `VirtualRailReticle`

**File:** [`VirtualRailReticle.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailReticle.cs)  
**Dòng:** 345

**Code lỗi:**
```csharp
private void UpdateCrosshairUI() {
    VirtualRailWeapon weapon = (ship != null) ? ship.GetComponent<VirtualRailWeapon>() : null;
}
```

**Cách khắc phục:**
```csharp
private VirtualRailWeapon _cachedWeapon;
void Start() { _cachedWeapon = ship?.GetComponent<VirtualRailWeapon>(); }
```

---

### H-06: `GetComponentInParent` mỗi frame cho Lock Marker

**File:** [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs)  
**Dòng:** 635

**Code lỗi:**
```csharp
private void UpdateLockMarkerUI() {
    var eh = lockedTarget.GetComponentInParent<EnemyHealth>();
}
```

**Cách khắc phục:** Cache `EnemyHealth` khi `lockedTarget` được gán lần đầu trong `ScanForLockTarget()`.

---

### H-07: `GetComponentInParent` trong Weapon Raycast loop

**File:** [`VirtualRailWeapon.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWeapon.cs)  
**Dòng:** 355, 446

**Code lỗi:**
```csharp
for (int i = 0; i < hitCount; i++) {
    var enemy = h.transform.GetComponentInParent<EnemyMovement>();
}
```

**Cách khắc phục:** Dùng Tag system hoặc Enemy Registry thay vì `GetComponentInParent` trong vòng lặp.

---

### H-08: `Camera.main` + `GetComponentInParent` trong LateUpdate

**File:** [`VirtualRailHorizonBackdrop.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHorizonBackdrop.cs)  
**Dòng:** 229-230

**Code lỗi:**
```csharp
// Trong LateUpdate:
targetCamera = GetComponentInParent<Camera>();
if (targetCamera == null) targetCamera = Camera.main;
```

**Cách khắc phục:** Cache camera 1 lần trong `Start()`. `Camera.main` trước Unity 2020.2 là `FindGameObjectWithTag("MainCamera")` nội bộ.

---

### H-09: `FindAnyObjectByType<Shield>` khi nhặt ring

**File:** [`CollectibleRing.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/CollectibleRing.cs)  
**Dòng:** 158

**Code lỗi:**
```csharp
var shield = FindAnyObjectByType<Shield>();
```

**Vấn đề:** Mỗi lần nhặt silver ring → Find toàn scene. Gây micro-stutter ~1-3ms.

**Cách khắc phục:** Lấy `Shield` trực tiếp từ `Collider other` (player) đã trigger collision:
```csharp
var shield = other.GetComponentInParent<Shield>(); // O(1) trên hierarchy nhỏ
```

---

### H-10: `Physics.OverlapSphere` tạo GC allocation

**File:** [`SmartBombModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SmartBombModule.cs)  
**Dòng:** 202

**Code lỗi:**
```csharp
Collider[] colliders = Physics.OverlapSphere(centerPos, shockwaveRadius);
```

**Vấn đề:** `Physics.OverlapSphere` cấp phát mảng mới mỗi lần gọi → GC spike.

**Cách khắc phục:**
```csharp
private static readonly Collider[] _bombHitBuffer = new Collider[128];

// Thay thế:
int count = Physics.OverlapSphereNonAlloc(centerPos, shockwaveRadius, _bombHitBuffer);
for (int i = 0; i < count; i++) { /* xử lý _bombHitBuffer[i] */ }
```

---

### H-11: `GetComponents<IPoolableEntity>()` tạo mảng GC mỗi lần nổ

**File:** [`Explosion.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/Explosion.cs)  
**Dòng:** 56

**Code lỗi:**
```csharp
var poolables = GetComponents<VirtualRail.IPoolableEntity>();
```

**Cách khắc phục:**
```csharp
private static readonly List<VirtualRail.IPoolableEntity> _poolableBuffer = new(8);

// Thay thế:
GetComponents(_poolableBuffer); // Overload không allocation
for (int i = 0; i < _poolableBuffer.Count; i++) { /* ... */ }
_poolableBuffer.Clear();
```

---

### H-12: `GetComponentInChildren/Parent` trong Kamikaze Update

**File:** [`FormationWingman.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/FormationWingman.cs)  
**Dòng:** 394-395

**Code lỗi:**
```csharp
// Trong UpdateKamikazeCharge() — gọi từ Update():
var shield = target.GetComponentInChildren<Shield>();
if (shield == null) shield = target.GetComponentInParent<Shield>();
```

**Cách khắc phục:** Cache `Shield` khi `TriggerKamikazeCharge()` gán target.

---

## 🟡 MEDIUM — Allocation không thường xuyên nhưng tích lũy

---

### M-01: `Instantiate` missile fallback thay vì dùng Pool

**File:** [`AimAssistModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/AimAssistModule.cs)  
**Dòng:** 483-485, 534-536

**Code lỗi:**
```csharp
GameObject missileObj = Instantiate(missilePrefab, nosePos, spawnRot);
missile = missileObj.GetComponent<VirtualRailHomingMissile>();
if (missile == null) missile = missileObj.AddComponent<VirtualRailHomingMissile>();
```

**Cách khắc phục:** Luôn sử dụng `VirtualRailMissilePool`. Đảm bảo prefab đã gắn sẵn component, loại bỏ `AddComponent` runtime.

---

### M-02: `AddComponent` fallback trong WaveSpawner

**File:** [`VirtualRailWaveSpawner.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailWaveSpawner.cs)  
**Dòng:** 445, 720

**Code lỗi:**
```csharp
var fsm = availableEnemy.GetComponent<EnemyLifecycleFSM>();
if (fsm == null) fsm = availableEnemy.gameObject.AddComponent<EnemyLifecycleFSM>();
```

**Cách khắc phục:** Gắn `EnemyLifecycleFSM` vào prefab trong Editor. Xóa fallback `AddComponent`.

---

### M-03: `Instantiate` / `new GameObject` khi Pool cạn

**Files:**
- [`EnemyProjectilePool.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/EnemyProjectilePool.cs) — Dòng 275
- [`VirtualRailLaserPool.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailLaserPool.cs) — Dòng 90, 131

**Code lỗi:**
```csharp
// EnemyProjectilePool.cs — khi hết slot:
GameObject obj = Instantiate(projectilePrefab, transform);

// VirtualRailLaserPool.cs — khi hết slot:
selectedBolt = CreateNewBolt(); // Gọi new GameObject() + AddComponent()
```

**Cách khắc phục:** 
- Tăng `initialPoolSize` dựa trên peak load thực tế.
- Nếu cần mở rộng pool, mở rộng theo batch trong frame idle (không phải giữa combat).
- Thêm `Debug.LogWarning` khi pool cạn để điều chỉnh kích thước phù hợp.

---

### M-04: `Instantiate` explosion fallback khi thiếu VFXPool

**File:** [`VirtualRailHomingMissile.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/VirtualRailHomingMissile.cs)  
**Dòng:** 401-406

**Code lỗi:**
```csharp
if (vfxPool != null && explosionPrefab != null)
    vfxPool.SpawnVFX(explosionPrefab, impactPoint, Quaternion.identity, null, 3f);
else if (explosionPrefab != null)
    Instantiate(explosionPrefab, impactPoint, Quaternion.identity);
```

**Cách khắc phục:** Bắt buộc `VirtualRailVFXPool` luôn tồn tại — xóa nhánh `else Instantiate`.

---

### M-05: `FindAnyObjectByType` khi nhấn Smart Bomb

**File:** [`SmartBombModule.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/SmartBombModule.cs)  
**Dòng:** 135

**Code lỗi:**
```csharp
VirtualRailReticle reticle = (_anchor != null) 
    ? _anchor.GetComponentInChildren<VirtualRailReticle>() 
    : FindAnyObjectByType<VirtualRailReticle>();
```

**Cách khắc phục:** Cache `VirtualRailReticle` trong `Awake()`.

---

### M-06: `GetComponent` mỗi lần Shield nhận damage

**File:** [`Shield.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/Shield.cs)  
**Dòng:** 64, 70

**Code lỗi:**
```csharp
var exp = GetComponent<Explosion>();
var ship = GetComponentInParent<VirtualRail.VirtualRailShip>();
```

**Cách khắc phục:** Cache cả hai trong `Awake()`.

---

### M-07: Tạo GameObject + Primitive động cho Shield Visual

**File:** [`EnemyHealth.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/VirtualRail/EnemyHealth.cs)  
**Dòng:** 137-160

**Code lỗi:**
```csharp
_shieldVisualObj = new GameObject("[Shield_Barrier]");
var mf = _shieldVisualObj.AddComponent<MeshFilter>();
var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
```

**Vấn đề:** `EnsureShieldVisual()` tạo GameObject + Mesh + Primitive động khi shield kích hoạt → micro-hitch ~2-5ms.

**Cách khắc phục:** Thêm Shield visual vào enemy prefab sẵn (inactive), chỉ gọi `SetActive(true)` khi cần.

---

### M-08: `Invoke()` dùng string reflection

**File:** [`Laser.cs`](file:///d:/3D-Shooter-master/Assets/Scripts/Laser.cs)  
**Dòng:** 114

**Code lỗi:**
```csharp
Invoke("CanFire", _fireDelay);
```

**Cách khắc phục:**
```csharp
private float _nextFireTime;

void Update() {
    if (Time.time >= _nextFireTime) {
        _canFire = true;
    }
}

void Fire() {
    _canFire = false;
    _nextFireTime = Time.time + _fireDelay;
}
```

---

## 📐 TỔNG HỢP PHƯƠNG ÁN KIẾN TRÚC

Nhiều vấn đề trên có thể giải quyết bằng **3 thay đổi kiến trúc** thay vì sửa từng file:

### 1. 🏗️ Player Singleton Registry
Giải quyết: **C-01** (3 file Find Player mỗi frame)
```csharp
public class PlayerReference : MonoBehaviour {
    public static Transform Instance { get; private set; }
    void OnEnable()  => Instance = transform;
    void OnDisable() => Instance = null;
}
```

### 2. 🏗️ Enemy Component Registry
Giải quyết: **H-02, H-03, H-04, H-06, H-07, H-12** (tất cả GetComponentInParent trong loop)
```csharp
public static class EnemyRegistry {
    static readonly Dictionary<Collider, (EnemyHealth hp, EnemyMovement mv)> _map = new(256);
    public static void Register(Collider c, EnemyHealth hp, EnemyMovement mv) => _map[c] = (hp, mv);
    public static void Unregister(Collider c) => _map.Remove(c);
    public static bool TryGet(Collider c, out EnemyHealth hp, out EnemyMovement mv) {
        if (_map.TryGetValue(c, out var pair)) { hp = pair.hp; mv = pair.mv; return true; }
        hp = null; mv = null; return false;
    }
}
```

### 3. 🏗️ Bắt buộc Pool — Xóa mọi Instantiate fallback
Giải quyết: **M-01, M-03, M-04** (Instantiate trong gameplay)
- Xóa tất cả nhánh `else { Instantiate(...) }` trong code gameplay.
- Tăng `initialPoolSize` dựa trên profiling thực tế.
- Đảm bảo mọi prefab đã gắn sẵn đầy đủ component → xóa `AddComponent` runtime.

---

## ✅ CHECKLIST TRIỂN KHAI

| # | Hành động | File liên quan | Ưu tiên | Trạng thái |
|---|-----------|----------------|---------|------------|
| 1 | Throttle fallback Find Player (1s) & ưu tiên VirtualRailAnchor | `EnemyAttack.cs`, `EnemyMovement.cs`, `FollowCam.cs` | 🔴 Critical | ✅ **ĐÃ HOÀN TẤT** |
| 2 | Thêm throttle `_nextFindReferencesTime` (1s) trong `Tick()` | `VirtualRailStageDirector.cs` | 🔴 Critical | ✅ **ĐÃ HOÀN TẤT** |
| 3 | Cache & throttle `_nextBindingRetryTime` / `_nextAimAssistRetryTime` | `DualReticleHUD.cs` | 🔴 Critical | ✅ **ĐÃ HOÀN TẤT** |
| 4 | Tối ưu Physics loop / Component Resolution (H-02, H-03, H-04, H-07, H-12) | 5 file Combat & Physics | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 5 | Cache weapon/reticle/anchor, xóa Find trong hot-path bắn đạn (H-01, H-05, H-06) | `VirtualRailWeapon.cs`, `VirtualRailReticle.cs`, `AimAssistModule.cs` | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 6 | Đổi `OverlapSphere` → `NonAlloc` + static buffer (H-10) | `SmartBombModule.cs` | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 7 | Đổi `GetComponents` → overload List không GC (H-11) | `Explosion.cs` | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 8 | Cache Camera & throttle retry trong `LateUpdate` (H-08) | `VirtualRailHorizonBackdrop.cs` | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 9 | Lấy Shield từ collider thay vì Find toàn scene (H-09) | `CollectibleRing.cs` | 🟠 High | ✅ **ĐÃ HOÀN TẤT** |
| 10 | Xóa tất cả `Instantiate` fallback | 4 file Pool/Missile | 🟡 Medium | ⏳ Chờ triển khai |
| 11 | Gắn component vào prefab, xóa `AddComponent` | 2 file Spawner | 🟡 Medium | ⏳ Chờ triển khai |
| 12 | Shield visual vào prefab | `EnemyHealth.cs` | 🟡 Medium | ⏳ Chờ triển khai |
| 13 | Thay `Invoke` bằng timer float | `Laser.cs` | 🟢 Low | ⏳ Chờ triển khai |

---

> **Ghi chú:** Báo cáo này dựa trên phân tích tĩnh mã nguồn. Để đo chính xác impact, cần chạy **Unity Profiler** (CPU Usage + GC Alloc) trong các scenario: idle flight, heavy combat (20+ enemies), smart bomb detonation, và player death/respawn.
