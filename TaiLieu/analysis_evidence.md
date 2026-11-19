# BẢNG PHÂN TÍCH EVIDENCE – HARVEST GRID

> Tạo từ: đọc source code thực tế trong `Assets/!_HarvestGrid/Scripts`  
> Mục đích: làm nền cho báo cáo V7 và bài báo khoa học

---

## 1. Bảng Feature Status

| Feature | Trạng thái | Bằng chứng (class / method) | Ghi chú |
|---------|-----------|---------------------------|---------|
| **Trồng cây (plant)** | ✅ Triển khai | `AddPlantUse.Apply()` → `FarmMono.Instance.AddPlant()` | Player KHÔNG trồng thủ công; player dùng **Item có UseType=AddPlant** (Seed item), item tự gọi `FarmMono.AddPlant()` |
| **Sinh trưởng cây (growth)** | ✅ Triển khai | `Plant.Absorb(Resource, int)` → `GrowToNextStage()` | Cây sinh trưởng khi nhận đủ Resource theo `nextStageRequirement`; mỗi PlantStage yêu cầu dict `{Resource: amount}` |
| **Thu hoạch (harvest)** | ✅ Triển khai | `HarvestUse.Apply()` → `FarmMono.Instance.HarvestRandom()` | Player dùng Item có UseType=Harvest (Scythe/LongScythe); `Plant.Harvest(float efficiency)` trả `Dictionary<Crop,int>` |
| **Hiệu suất thu hoạch** | ✅ Triển khai | `Plant.Harvest(float efficiency)` | `efficiency` ảnh hưởng yield và star quality; `HarvestUse.effective` là config từ ItemSO |
| **ProvideResource (buff items)** | ✅ Triển khai | `ProviceResourceUse.cs` | Item loại này cung cấp Resource cho plant; bao gồm WaterCan, Fertilizer, GrowthBuff, Lantern, MegaBuff |
| **Farm Slot nâng cấp** | ✅ Có UI | `FarmSlot.level`, UI: `SlotUpgradePanel` | FarmSlot có field `level`; có panel `SlotUpgradePanel` → xác nhận chức năng tồn tại |
| **Inventory Grid (placement)** | ✅ Triển khai | `StoringSpace.cs`, `Footprint.cs`, `ItemFootprintSO` | Lưới 2D, item chiếm footprint dạng bool[,]; hỗ trợ rotate |
| **Item Use (factory pattern)** | ✅ Triển khai | `ItemUseFactory.cs`, `ItemUse` abstract, concrete: `AddPlantUse`, `HarvestUse`, `ProviceResourceUse` | 3 loại ItemUse confirmed |
| **Shop** | ✅ Có UI | `ShopPanel` UI, `ItemManager.cs`, `LevelSO.itemPool` | Shop pool xác suất cấu hình trong `LevelSO.itemAndChancePool` |
| **Shop AI / Item Suggestion** | ✅ Triển khai | `GoalItemSuggestionManager.cs` | Không phải ML/training; là **rule-based goal-driven suggestion** — xem chi tiết mục 4 |
| **DDA (Dynamic Difficulty Adjustment)** | ❌ Chưa tìm thấy | Không có class DDA, không có class điều chỉnh difficulty | Có thể là thiết kế/định hướng; `LevelSO` có `timeBudget` và `itemPool.chance` nhưng không có runtime DDA |
| **Save local** | ✅ Triển khai | `GameManager.Save()` → `DataPersistenceManager.Instance.SaveGame(saveSlotId)` | Dùng `DataPersistenceManager` từ `LgTyLib.Modules.DataPersistence` |
| **Load local** | ✅ Triển khai | `GameManager.Load()` → `DataPersistenceManager.Instance.LoadGame(saveSlotId)` | Khởi động: `UserManager.Instance.Load()` |
| **PlayFab Auth (login)** | ✅ Triển khai | `PlayFabAuthService.Login()` → `PlayFabClientAPI.LoginWithPlayFab()` | Title ID: `14B566`; sau login → `DataPersistenceManager.Instance.LoadCloudSaves()` |
| **PlayFab Cloud Save** | ✅ Triển khai | `PlayFabAuthService` → `LoadCloudSaves()` | Cloud save được load sau khi login thành công |
| **Guest/Local mode** | ✅ Triển khai | `LocalAuthService.cs` (tồn tại) | Code có `LocalAuthService` nhưng runtime dùng `PlayFabAuthService` |
| **Settings** | ✅ Triển khai | `SettingPanel.cs`, `GraphicsSettingsManager.cs` | Graphics settings manager xác nhận |
| **Profile / User** | ✅ Triển khai | `UserManager.cs`, `UserProfilePopup.cs`, `UserProfileButton.cs` | Có UI popup và button |
| **Leaderboard** | ✅ Triển khai | `LeaderboardManager.cs`, `LeaderboardUI.cs`, PlayFab integration | Leaderboard qua PlayFab |
| **Cosmetics** | ✅ Triển khai | `CosmeticManager.cs`, `CosmeticPanel.cs`, `CosmeticData.cs` | Hệ thống trang trí riêng |

---

## 2. Bảng ScriptableObject

| SO Class | Assets thực tế | Mục đích | Fields chính |
|---------|-----------|---------|-------------|
| **LevelSO** | `LevelSO.asset`, `LevelSO 1–4.asset` (5 levels) | Cấu hình màn chơi | `levelID`, `timeBudget`, `money`, `requiredCrops[]`, `itemPool[]` (chance weights), `storingSpaceSO`, `farmMono` |
| **CropSO** | `Bean`, `Cucumber`, `Tomato`, `Wheat` | Cấu hình nông sản | `crop` (Crop object) |
| **ItemFootprintSO** | `Growth-fp`, `LightBuff-fp`, `LongScythe-fp`, `Scythe-fp`, `SeedPack`, `WaterCan-fp` | Footprint shape của item trên lưới | `width`, `height`, `grid` (bool[]), `footprint` (Footprint) |
| **ItemSO** (inferred) | `Fertilizer`, `GrowthBuff`, `Lantern`, `LongScythe`, `MegaBuff`, `Scythe`, `Seed_Bean/Cucumber/Tomato/Wheat`, `WaterCan` | Cấu hình vật phẩm | ItemUse config, ItemRole, rarity, icon, footprint ref |
| **StoringSpaceSO** | `StoringSpaceSO.asset`, `StoringSpaceSO 1.asset` | Cấu hình lưới kho (kích thước và layout) | grid dimensions, cell config |
| **GameplaySceneDataSO** | `GameplaySceneDataSO.asset` | Config chung cho gameplay scene | Tham chiếu từ `GameManager.gameplaySceneDataSO` |
| **PlantSO/PlantPrototype** | `Bean`, `Cucumber`, `Tomato`, `Wheat` (trong `/Plants`) | Cấu hình cây trồng và các giai đoạn | `PlantPrototype.cs` |

---

## 3. Dynamic Data / JSON Structure

| Nhóm dữ liệu | Fields chính | Nơi lưu | Hệ thống đọc/ghi |
|-------------|------------|---------|----------------|
| **UserData** | `userId`, `username` | Local + PlayFab | `AuthManager`, `DataPersistenceManager` |
| **Inventory** | `money` (int), `crops` (Dict<Crop,int>), `storedItems` (List<StoredObject>) | Local JSON | `InventoryMono`, `DataPersistenceManager` |
| **Farm** | `farmSlots` (List<FarmSlot>) | Local JSON | `FarmMono`, `DataPersistenceManager` |
| **FarmSlot** | `slotID`, `soilQuality`, `soilMoisture`, `plant`, `level`, `beingEffected` | Local JSON (nested trong Farm) | `FarmMono` |
| **Plant** | `plantID`, `plantName`, `averageCropCount`, `currentState` (PlantStage), `stages` (remaining List), `nextStageRequirement` (Dict<Resource,int>), `crop` | Local JSON (nested trong FarmSlot) | `FarmMono` |
| **PlayTime** | play session timing | Local | `TimeManager` |
| **LeaderboardData** | scores, rankings | PlayFab cloud | `LeaderboardManager` |
| **Settings** | graphics/audio settings | Local | `GraphicsSettingsManager` |

---

## 4. AI / GoalItemSuggestionManager — Phân tích Chi Tiết

### Loại: Rule-Based Goal-Driven Item Suggestion (KHÔNG phải Machine Learning)

**Mô tả:** Hệ thống phân tích mục tiêu màn chơi và inventory hiện tại, sau đó **hiển thị "ghost" (bóng mờ) của item cần mua** trực tiếp trên lưới inventory để gợi ý người chơi.

### Input Features:
| Feature | Nguồn | Mô tả |
|---------|-------|-------|
| `levelSO.requiringCrops` | LevelSO (Static Data) | Danh sách cây cần thu hoạch và số lượng mục tiêu |
| `Inventory.GetItemList()` | Dynamic - Inventory | Danh sách item hiện có trong kho |
| `Inventory.Crops` | Dynamic - Inventory | Số lượng crops đã thu hoạch |
| `StoringSpace.Cells` | Dynamic - Grid state | Trạng thái ô lưới (trống/chiếm) để tìm vị trí ghost |

### Decision Rules:
```
Priority roles: [Seed, Watering, HarvestTool, Fertilizer]

For each requiredCrop in levelSO.requiringCrops:
    if currentHarvested >= goalAmount: skip (goal met)
    goalIncomplete = true
    
    For each role in priorityRoles:
        candidates = CropItemRegistry.GetItemsForCrop(crop, role)
        if player has NONE of these candidates in inventory:
            → add first candidate to missingCandidates

For each missing candidate (not yet shown):
    Find first-fit grid placement (try 4 rotations via Footprint.Rotate())
    if placement found: spawn visual ghost at that location
```

### Output:
- Visual ghost overlay trên inventory grid (màu vàng-trong suốt, `GhostPulseAnimation`)
- **Không ảnh hưởng shop content, không điều chỉnh difficulty, không thay đổi gameplay params**

### Trigger:
- `SuggestionLoop()`: re-evaluate mỗi **45 giây**
- `Inventory.OnItemsChanged` event
- `Inventory.OnCropsChanged` event

### Phân biệt với DDA:
| | GoalItemSuggestionManager | DDA |
|--|--------------------------|-----|
| Input | Inventory, Crops, Level goals | Player performance, difficulty metrics |
| Output | Visual UI hint | Gameplay parameter adjustment |
| Trạng thái | ✅ Triển khai | ❌ Chưa tìm thấy trong codebase |

---

## 5. Danh sách Câu Hỏi Chưa Xác Minh

1. **Auto-Farming thực sự là gì?** Plant cần Resource để lên stage — nguồn Resource đến từ player dùng item (ProviceResourceUse) hay có timer/auto-tick? (`ProgressTimer.cs` cần đọc)
2. **Farm Slot mua/nâng cấp**: `SlotUpgradePanel` tồn tại, `FarmSlot.level` tồn tại — giá và điều kiện upgrade lấy từ đâu?
3. **DDA**: Không tìm thấy trong codebase — cần xác nhận là thiết kế/đề xuất chưa triển khai.
4. **Shop Manager**: Không có `ShopManager.cs` riêng — Shop được quản lý bởi `ItemManager.cs` hay UI panel trực tiếp?
5. **PlantSO/PlantPrototype**: Cấu trúc dữ liệu PlantSO thực tế như thế nào? Cần đọc `PlantPrototype.cs`.
6. **Offline mode**: Có cơ chế xử lý offline không (khi PlayFab không kết nối)?
7. **LLMUnity**: Project có package LLMUnity — có được tích hợp vào gameplay không?
