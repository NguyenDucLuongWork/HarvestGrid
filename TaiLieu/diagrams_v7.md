# SƠ ĐỒ HARVEST GRID – V7

> Tất cả sơ đồ dưới đây được tạo từ source code thực tế.  
> Tên class/component khớp với codebase.

---

## Diagram 1 – Use Case Tổng Thể

```mermaid
flowchart LR
    Player(["👤 Player"])
    System(["⚙️ System\n(Auto)"])

    subgraph UC["Harvest Grid – Use Cases"]
        direction TB

        subgraph AUTH["Xác thực"]
            UC1["Đăng ký / Đăng nhập\n(PlayFab)"]
            UC2["Chơi không cần đăng nhập\n(Local/Guest)"]
            UC3["Đăng xuất"]
        end

        subgraph HOME["Màn hình chính"]
            UC4["Xem danh sách màn chơi"]
            UC5["Chọn màn chơi"]
            UC6["Xem Profile người chơi"]
            UC7["Xem bảng xếp hạng\n(Leaderboard)"]
            UC8["Cài đặt\n(Graphics/Sound)"]
        end

        subgraph GAMEPLAY["Gameplay"]
            UC9["Xem Farm Grid\n(trạng thái cây trồng)"]
            UC10["Dùng Seed item\n→ Trồng cây (AddPlantUse)"]
            UC11["Dùng Tool item\n→ Thu hoạch (HarvestUse)"]
            UC12["Dùng Buff item\n→ Cung cấp Resource (ProviceResourceUse)"]
            UC13["Xem và quản lý\nInventory Grid"]
            UC14["Xem mục tiêu màn chơi"]
            UC15["Thắng / Thua màn chơi"]
        end

        subgraph SHOP["Shop"]
            UC16["Xem vật phẩm trong Shop"]
            UC17["Mua vật phẩm"]
        end

        subgraph FARM_MGMT["Quản lý Nông trại"]
            UC18["Xem trạng thái Farm Slot"]
            UC19["Nâng cấp Farm Slot\n(SlotUpgradePanel)"]
        end

        subgraph SYSTEM_AUTO["Hệ thống Tự động"]
            UC20["Sinh trưởng cây theo Resource\n(Plant.Absorb → GrowToNextStage)"]
            UC21["Gợi ý vật phẩm cần mua\n(GoalItemSuggestionManager)"]
            UC22["Lưu / Tải dữ liệu\n(DataPersistenceManager)"]
        end
    end

    Player --> UC1
    Player --> UC2
    Player --> UC3
    Player --> UC4
    Player --> UC5
    Player --> UC6
    Player --> UC7
    Player --> UC8
    Player --> UC9
    Player --> UC10
    Player --> UC11
    Player --> UC12
    Player --> UC13
    Player --> UC14
    Player --> UC15
    Player --> UC16
    Player --> UC17
    Player --> UC18
    Player --> UC19

    System --> UC20
    System --> UC21
    System --> UC22
```

---

## Diagram 2 – Logical / System Architecture

```mermaid
flowchart TD
    subgraph UILayer["UI Layer"]
        UI_Login["LoginScene"]
        UI_Home["HomePanel\nLevelGroup / LevelButton\nLeaderboardUI\nUserProfilePopup"]
        UI_Game["GameplayScene\nInventoryGrid / FarmGrid\nShopPanel / SlotUpgradePanel"]
        UI_Setting["SettingPanel / CosmeticPanel"]
    end

    subgraph LogicLayer["Game Logic Layer - Managers"]
        GM["GameManager\n(orchestration)"]
        AM["AuthManager\n+ PlayFabAuthService"]
        LM["LevelsManager"]
        IM["ItemManager"]
        FM["FarmMono"]
        InvM["InventoryMono"]
        GISM["GoalItemSuggestionManager\n(AI hint - rule-based)"]
        LBM["LeaderboardManager"]
        GSM["GraphicsSettingsManager"]
        DPM["DataPersistenceManager"]
    end

    subgraph DataLayer["Data Layer"]
        subgraph StaticData["Static Data - ScriptableObjects"]
            LevelSO["LevelSO"]
            ItemSO["ItemSO"]
            PlantSO["PlantSO"]
            CropSO["CropSO"]
        end
        subgraph DynamicData["Dynamic Data - Runtime"]
            InvData["Inventory\n(money, crops, items)"]
            FarmData["Farm → FarmSlot → Plant"]
            UserData["UserData\n(userId, username)"]
        end
    end

    UILayer -->|user events| LogicLayer
    LogicLayer -->|reads config| StaticData
    LogicLayer -->|reads/writes| DynamicData
    DPM -->|local JSON| LocalSave[("Local JSON")]
    DPM -->|cloud sync| PlayFab[("PlayFab Cloud")]
```

---

## Diagram 3 – Physical / Deployment Architecture

```mermaid
flowchart LR
    Player(["Player"])

    subgraph Android["Android Device"]
        direction TB
        subgraph UnityApp["Unity App - HarvestGrid.apk"]
            UI["UI Layer UGUI"]
            Logic["Game Systems & Managers C# runtime"]
            AIRuntime["GoalItemSuggestionManager\nrule-based runs LOCAL"]
            SO_Pack["ScriptableObjects packed\nLevelSO, ItemSO, PlantSO, CropSO"]
        end
        LocalJSON[("JSON Save Files\nDataPersistenceManager")]
    end

    subgraph Cloud["PlayFab Cloud"]
        PF_Auth["PlayFab Auth"]
        PF_Save["PlayFab Cloud Save\nPlayer Data"]
        PF_LB["PlayFab Leaderboard"]
    end

    Player -->|touch input| UI
    UI --> Logic
    Logic --> AIRuntime
    Logic --> SO_Pack
    Logic <-->|read/write| LocalJSON
    Logic -->|HTTPS API PlayFab SDK| PF_Auth
    Logic -->|LoadCloudSaves\nSaveCloudData| PF_Save
    Logic -->|Submit/Fetch scores| PF_LB
```

> **Lưu ý quan trọng:** Toàn bộ game logic, AI inference và rendering chạy trên Android device. PlayFab chỉ phục vụ xác thực, lưu cloud và bảng xếp hạng.

---

## Diagram 4 – Data Architecture Overview

```mermaid
flowchart TD
    subgraph Static["Static Data - ScriptableObjects packed with app"]
        LevelSO["LevelSO\nlevelID, timeBudget, money\nrequiredCrops[], itemPool[]"]
        ItemSO["ItemSO\nItemRole, ItemRarity, Icon\nItemUse config, Footprint ref"]
        PlantProto["PlantSO / PlantPrototype\nstages: List of PlantStage\neach stage: requiredResources"]
        CropSO["CropSO\nCrop definition ID, name, sprite"]
    end

    subgraph Runtime["Runtime Systems"]
        FarmMono["FarmMono"]
        InvMono["InventoryMono"]
        GISM["GoalItemSuggestionManager"]
        ItemMgr["ItemManager"]
        DPM["DataPersistenceManager"]
    end

    subgraph Dynamic["Dynamic Data - Runtime State"]
        FarmState["Farm\nFarmSlot slotID, level, soil\nPlant plantID, currentStage\nnextStageRequirement, crop"]
        InvState["Inventory\nmoney int\ncrops Dict Crop int\nstoredItems List StoredObject"]
        UserState["UserData\nuserId, username"]
    end

    subgraph Persist["Persistence"]
        LocalJSON[("Local JSON")]
        PlayFabCloud[("PlayFab Cloud")]
    end

    Static -->|configure| Runtime
    Runtime -->|read/write| Dynamic
    Dynamic -->|serialize| LocalJSON
    LocalJSON -->|load on start| Dynamic
    Dynamic -->|cloud sync on login| PlayFabCloud
    PlayFabCloud -->|load after auth| Dynamic

    GISM -->|reads| InvState
    GISM -->|reads goal| LevelSO
    GISM -->|shows ghost hint| InvMono
```

---

## Diagram 5 – ScriptableObject Model

```mermaid
classDiagram
    class LevelSO {
        +int levelID
        +float timeBudget
        +int money
        +List~CropRequirement~ requiredCrops
        +List~ItemChanceEntry~ itemPool
        +StoringSpaceSO storingSpaceSO
    }
    class CropRequirement {
        +string CropID
        +int stars
        +int amount
    }
    class ItemChanceEntry {
        +string ItemID
        +float chance
    }
    class ItemSO {
        +string Id
        +string Name
        +ItemRole Role
        +ItemRarity Rarity
        +Sprite Icon
        +ItemFootprintSO footprintSO
    }
    class ItemFootprintSO {
        +int width
        +int height
        +bool[] grid
        +Footprint footprint
        +SaveToFootprint()
    }
    class CropSO {
        +Crop crop
    }
    class StoringSpaceSO {
        +int gridWidth
        +int gridHeight
    }

    LevelSO "1" --> "many" CropRequirement
    LevelSO "1" --> "many" ItemChanceEntry
    LevelSO --> StoringSpaceSO
    ItemSO --> ItemFootprintSO
```

---

## Diagram 6 – Entity / Data Class Diagram

```mermaid
classDiagram
    class Farm {
        +List~FarmSlot~ FarmSlots
        +GetEmptySlot() FarmSlot
        +GetPlants() List~Plant~
        +GetHarvestablePlants() List~Plant~
        +ValidateData() int
    }
    class FarmSlot {
        +string SlotID
        +SoilQuality soilQuality
        +SoilMoisture soilMoisture
        +int level
        +bool beingEffected
        +Plant plant
        +AddPlant(plant) bool
        +RemovePlant(plant) bool
        +IsEmpty() bool
    }
    class Plant {
        +string PlantID
        +string PlantName
        +int averageCropCount
        +PlantStage currentState
        +List~PlantStage~ stages
        +bool IsFullyGrown
        +BeginGrow(go) bool
        +GrowToNextStage() bool
        +Absorb(resource, amount) int
        +Harvest(efficiency) Dict
    }
    class PlantStage {
        +Sprite sprite
        +Dict~Resource,int~ RequiredResources
        +Clone() PlantStage
    }
    class Crop {
        +string CropID
        +int Stars
        +SetStars(int)
        +Clone() Crop
    }
    class Inventory {
        +int Money
        +Dict~Crop,int~ Crops
        +List~StoredObject~ storedItems
        +AddItem(StoredObject)
        +AddCrop(crop, amount)
        +AddMoney(amount)
        +GetItemList() List~Item~
    }
    class StoredObject {
        +Item Item
        +Vector2Int Position
        +int Rotation
    }
    class Item {
        +string Id
        +string Name
        +ItemRole Role
        +Footprint Footprint
    }
    class Footprint {
        +bool[,] Requiring
        +Rotate()
        +Clone() Footprint
    }

    Farm "1" --> "many" FarmSlot
    FarmSlot "1" --> "0..1" Plant
    Plant "1" --> "many" PlantStage : stages remaining
    Plant --> PlantStage : currentState
    Plant "1" --> "1" Crop
    Inventory "1" --> "many" StoredObject
    StoredObject "1" --> "1" Item
    Item "1" --> "1" Footprint
```

---

## Diagram 7 – Item Use System

```mermaid
classDiagram
    class ItemUse {
        <<abstract>>
        +ItemUseType UseType
        +Apply(ItemUseContext ctx)*
    }
    class AddPlantUse {
        +UseType: AddPlant
        +Plant plantToAdd
        +Apply: FarmMono.AddPlant
    }
    class HarvestUse {
        +UseType: Harvest
        +float effective
        +Apply: FarmMono.HarvestRandom
    }
    class ProviceResourceUse {
        +UseType: ProviceResource
        +Resource resourceType
        +int amount
        +Apply: Plant.Absorb
    }
    class ItemUseFactory {
        +Create(type) ItemUse
    }
    class ItemUseContext {
        +FarmSlot TargetSlot
        +Item SourceItem
    }

    ItemUse <|-- AddPlantUse
    ItemUse <|-- HarvestUse
    ItemUse <|-- ProviceResourceUse
    ItemUseFactory ..> ItemUse : creates
    ItemUse --> ItemUseContext : uses
```

---

## Diagram 8 – Auto-Farming / Plant Growth Activity

```mermaid
flowchart TD
    Start(["Player chọn Seed item"])
    UseItem["Player kích hoạt Item Use\ntap seed → tap FarmSlot"]
    AddPlant["AddPlantUse.Apply\nFarmMono.AddPlant(plant, slot)"]
    BeginGrow["Plant.BeginGrow\nGrowToNextStage Stage 0 to 1"]
    ShowPlant["PlantMono.UpdateSprite\nHiển thị cây stage 1"]

    CheckReq{{"Plant cần Resource\n(nextStageRequirement)?"}}

    UseBuffItem["Player dùng Buff Item\nWaterCan/Fertilizer/GrowthBuff"]
    ProvideRes["ProviceResourceUse.Apply\nPlant.Absorb(resource, amount)"]
    FulfillReq{"nextStageRequirement\nđã thỏa đủ?"}
    GrowStage["Plant.GrowToNextStage\ncurrentState = stages.RemoveFirst\nTrigger OnStageChanged"]
    UpdateUI["PlantMono.UpdateSprite\nPlantMono.UpdateRequiringPanel"]

    FullyGrown{"Plant.IsFullyGrown?\nstages.Count == 0"}

    UseHarvest["Player dùng Harvest Tool\nScythe / LongScythe"]
    Harvest["HarvestUse.Apply(effective)\nPlant.Harvest(efficiency)"]
    CalcYield["totalYield = averageCropCount x efficiency\nbaseStar = Clamp(Floor(eff x 2), 1, 5)"]
    AddCrops["Inventory.AddCrop\nTrigger OnCropsChanged"]
    ClearSlot["FarmSlot.RemovePlant\nSlot trống sẵn sàng dùng lại"]
    WinCheck{"Tất cả requiredCrops\nđã đủ?"}
    Win(["GameplayScene.Win"])
    Continue(["Tiếp tục gameplay"])

    Start --> UseItem
    UseItem --> AddPlant
    AddPlant --> BeginGrow
    BeginGrow --> ShowPlant
    ShowPlant --> CheckReq

    CheckReq -->|Có| UseBuffItem
    CheckReq -->|Không - fully grown| FullyGrown

    UseBuffItem --> ProvideRes
    ProvideRes --> FulfillReq
    FulfillReq -->|Chưa đủ| CheckReq
    FulfillReq -->|Đủ| GrowStage
    GrowStage --> UpdateUI
    UpdateUI --> FullyGrown

    FullyGrown -->|Chưa| CheckReq
    FullyGrown -->|Có| UseHarvest
    UseHarvest --> Harvest
    Harvest --> CalcYield
    CalcYield --> AddCrops
    AddCrops --> ClearSlot
    ClearSlot --> WinCheck
    WinCheck -->|Chưa đủ| Continue
    WinCheck -->|Đủ| Win
```

---

## Diagram 9 – AI Goal Item Suggestion Pipeline

```mermaid
flowchart TD
    subgraph Input["Input - đọc từ runtime state"]
        I1["LevelSO.requiringCrops\ngoal crops + amount"]
        I2["Inventory.Crops\nđã thu hoạch bao nhiêu"]
        I3["Inventory.GetItemList\nitem hiện có trong kho"]
        I4["StoringSpace.Cells\nô lưới trống/chiếm"]
    end

    subgraph Decision["Decision Logic - Rule-Based chạy LOCAL trên Android"]
        D1["Foreach requiredCrop:\nSo sánh currentHarvested vs goalAmount"]
        D2{"Goal đã\nhoàn thành?"}
        D3["Foreach priorityRole\nSeed - Watering - HarvestTool - Fertilizer"]
        D4["CropItemRegistry.GetItemsForCrop"]
        D5{"Player đã có\nitem này?"}
        D6["Add to missingCandidates"]
        D7["Lấy candidate đầu tiên\nchưa được hiển thị"]
        D8["First-fit placement\nthử 4 rotation của Footprint"]
        D9{"Tìm được\nvị trí đặt?"}
    end

    subgraph Output["Output"]
        O1["Hiển thị Visual Ghost\nmàu vàng bán trong suốt\ntrên Inventory Grid"]
        O2["GhostPulseAnimation\nanimation nhấp nhô"]
        O4["Không hiển thị\nkhông có chỗ trống"]
        O5["ClearAllSuggestions"]
    end

    subgraph Trigger["Trigger"]
        T1["SuggestionLoop\nmỗi 45 giây"]
        T2["Inventory.OnItemsChanged"]
        T3["Inventory.OnCropsChanged"]
    end

    T1 --> D1
    T2 --> D1
    T3 --> D1
    I1 --> D1
    I2 --> D1
    D1 --> D2
    D2 -->|Có| O5
    D2 -->|Chưa| D3
    I3 --> D5
    D3 --> D4
    D4 --> D5
    D5 -->|Có| D3
    D5 -->|Không| D6
    D6 --> D7
    D7 --> D8
    I4 --> D8
    D8 --> D9
    D9 -->|Có| O1
    D9 -->|Không| O4
    O1 --> O2
```

> ⚠️ **Lưu ý:** Đây là **rule-based decision system**, KHÔNG phải machine learning. Không có training, model weights hay dữ liệu lịch sử người chơi được dùng để cập nhật logic.

---

## Diagram 10 – Login / Save / Load Sequence

```mermaid
sequenceDiagram
    actor Player
    participant UI as Login UI
    participant AM as AuthManager
    participant PF as PlayFabAuthService
    participant DPM as DataPersistenceManager
    participant Local as Local JSON
    participant Cloud as PlayFab Cloud

    Note over Player,Cloud: CASE 1 - Local / Guest Mode
    Player->>UI: Chơi không đăng nhập
    UI->>AM: SetPendingLogin
    AM->>DPM: LoadGame(saveSlotId)
    DPM->>Local: Đọc file JSON
    Local-->>DPM: UserData, Inventory, Farm
    DPM-->>AM: LoadUserData(data)
    Note right of AM: AuthSession.Set(userId, username, local_token)

    Note over Player,Cloud: CASE 2 - PlayFab Login
    Player->>UI: Nhập username + password
    UI->>AM: Trigger login
    AM->>PF: Login(username, password)
    PF->>Cloud: PlayFabClientAPI.LoginWithPlayFab
    Cloud-->>PF: PlayFabId, SessionTicket
    PF->>AM: AuthSession.Set(PlayFabId, username, ticket)
    PF->>DPM: SetCurrentPlayer
    PF->>DPM: LoadCloudSaves
    DPM->>Cloud: Fetch Player Data
    Cloud-->>DPM: Cloud save data
    DPM-->>UI: onSuccess callback
    UI->>UI: Chuyển sang HomeScene

    Note over Player,Cloud: SAVE - khi kết thúc màn
    Player->>UI: Kết thúc màn chơi
    UI->>AM: GameManager.Save
    AM->>DPM: SaveGame(saveSlotId)
    DPM->>Local: Ghi JSON
    opt PlayFab đã đăng nhập
        DPM->>Cloud: Sync cloud save
    end
```

---

## Bảng đối chiếu: Use Case ↔ Manager ↔ UI Screen ↔ Data

| Use Case | Manager / System | UI Screen | Static Data | Dynamic Data |
|----------|-----------------|-----------|------------|-------------|
| Đăng nhập PlayFab | `AuthManager`, `PlayFabAuthService` | `LoginScene` | — | `UserData` |
| Chơi Guest | `AuthManager`, `LocalAuthService` | `LoginScene` | — | `UserData` local |
| Xem màn chơi | `LevelsManager` | `HomePanel > LevelGroup` | `LevelSO[]` | Level unlock state |
| Trồng cây | `FarmMono`, `InventoryMono` | `GameplayScene` | `PlantSO`, `ItemSO` | `Farm`, `Inventory` |
| Thu hoạch | `FarmMono`, `InventoryMono` | `GameplayScene` | `ItemSO (HarvestTool)` | `Farm`, `Inventory.crops` |
| Dùng Buff item | `FarmMono`, `InventoryMono` | `GameplayScene` | `ItemSO (Buff)` | `Plant.nextStageRequirement` |
| Quản lý Inventory | `InventoryMono` | `GameplayScene > InventoryGrid` | `ItemFootprintSO` | `Inventory.storedItems` |
| Mua vật phẩm | `ItemManager` | `ShopPanel` | `LevelSO.itemPool` | `Inventory.money` |
| Nâng cấp Farm Slot | — (cần xác minh) | `SlotUpgradePanel` | — | `FarmSlot.level` |
| AI Gợi ý vật phẩm | `GoalItemSuggestionManager` | Overlay trên `InventoryGrid` | `LevelSO.requiredCrops` | `Inventory`, `StoringSpace.Cells` |
| Cài đặt | `GraphicsSettingsManager` | `SettingPanel` | — | Settings state |
| Xem Profile | `UserManager` | `UserProfilePopup` | — | `UserData` |
| Xem Leaderboard | `LeaderboardManager` | `LeaderboardUI` | — | PlayFab Leaderboard |
| Lưu / Tải | `DataPersistenceManager` | — background | — | Tất cả dynamic data |
