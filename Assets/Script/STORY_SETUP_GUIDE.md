# Hướng Dẫn Setup Cốt Truyện (Story System)

## Tổng Quan

Hệ thống cốt truyện hỗ trợ **7 cảnh story**:
1. **Character Intro**: Cảnh giới thiệu nhân vật (trước MainMenu) - Story ID: `"CharacterIntro"`
2. **Level 1 Intro**: Cảnh mở đầu Level 1 - Story ID: `"Level1_Intro"` (hoặc tùy chỉnh)
3. **Level 1 Outro**: Cảnh kết thúc Level 1 - Story ID: `"Level1_Outro"` (hoặc tùy chỉnh)
4. **Level 2 Intro**: Cảnh mở đầu Level 2 - Story ID: `"Level2_Intro"` (hoặc tùy chỉnh)
5. **Level 2 Outro**: Cảnh kết thúc Level 2 - Story ID: `"Level2_Outro"` (hoặc tùy chỉnh)
6. **Level 3 Intro**: Cảnh mở đầu Level 3 - Story ID: `"Level3_Intro"` (hoặc tùy chỉnh)
7. **Level 3 Outro**: Cảnh kết thúc Level 3 - Story ID: `"Level3_Outro"` (hoặc tùy chỉnh)

## Bước 1: Tạo Story Scenes

1. **Tạo Character Intro Scene:**
   - `File` → `New Scene`
   - Lưu tại: `Assets/Scenes/CharacterIntro.unity`
   - Scene này sẽ hiển thị trước MainMenu (chỉ 1 lần khi game khởi động lần đầu)

2. **Tạo Story Screen Scene (dùng chung cho tất cả story):**
   - `File` → `New Scene`
   - Lưu tại: `Assets/Scenes/StoryScreen.unity`
   - Scene này sẽ được dùng cho tất cả story intro/outro của các level

2. **Setup Canvas:**
   - Tạo Canvas (nếu chưa có): `GameObject` → `UI` → `Canvas`
   - Render Mode: `Screen Space - Overlay` (hoặc `Screen Space - Camera` nếu bạn dùng camera)
   - Thêm `GraphicRaycaster` component

3. **Tạo Story Panel:**
   - `GameObject` → `UI` → `Panel` (đặt tên: `StoryPanel`)
   - Đặt làm child của Canvas
   - Có thể thêm background image/sprite nếu muốn

4. **Tạo UI Elements trong StoryPanel:**
   
   **a) Background Panel (Right Side - Optional):**
   - `GameObject` → `UI` → `Panel` hoặc `Image` (đặt tên: `BackgroundPanel` hoặc `StoryImage`)
   - Đặt trong StoryPanel, bên phải màn hình
   - Có thể chứa background scene, window view, hoặc story illustration
   - Có thể dùng `StoryImage` field trong StoryManager nếu muốn
   
   **b) Speaker Portrait Image:**
   - `GameObject` → `UI` → `Image` (đặt tên: `SpeakerPortraitImage`)
   - Đặt trong StoryPanel, bên trái màn hình (gần dialogue box)
   - Position: Left side, align với dialogue box
   - Size: Khoảng 200x200 hoặc tùy chỉnh theo layout
   - Preserve Aspect: Bật
   - Đây là ảnh đại diện của nhân vật đang nói
   - Portrait sẽ thay đổi theo người nói
   
   **c) Dialogue Box (Speech Bubble):**
   - `GameObject` → `UI` → `Panel` (đặt tên: `DialogueBox`)
   - Đặt trong StoryPanel, bottom-center
   - Tạo hình dạng speech bubble (có thể dùng Image với sprite có tail chỉ về bên trái)
   - Background: Màu beige/nhạt, có border
   - Position: Bottom-center, có thể có tail chỉ về portrait bên trái
   
   **d) Speaker Name Text:**
   - `GameObject` → `UI` → `Text - TextMeshPro` (đặt tên: `SpeakerNameText`)
   - Đặt trong `DialogueBox`, phía trên dialogue text
   - Font size: 28-32 (tùy chỉnh)
   - Alignment: Left hoặc Center
   - Text: Tên nhân vật (ví dụ: "Nhân vật A", "Nhân vật B")
   
   **e) Dialogue Text:**
   - `GameObject` → `UI` → `Text - TextMeshPro` (đặt tên: `StoryText`)
   - Đặt trong `DialogueBox`, phía dưới speaker name
   - Font size: 24-28 (tùy chỉnh)
   - Alignment: Left hoặc Center
   - Wrap text: Bật
   - Padding: Thêm padding để text không sát viền dialogue box
   
   **f) Continue Button:**
   - `GameObject` → `UI` → `Button - TextMeshPro` (đặt tên: `ContinueButton`)
   - Đặt trong `DialogueBox` hoặc StoryPanel
   - Text: "Continue" hoặc "Continue>"
   - Position: Bottom-right của dialogue box hoặc bottom-center
   - Style: Có thể match với style pixel art của game
   
   **g) Skip Button:**
   - `GameObject` → `UI` → `Button - TextMeshPro` (đặt tên: `SkipButton`)
   - Đặt trong StoryPanel hoặc Background Panel
   - Text: "Skip" hoặc "Skip>"
   - Position: Top-right corner (có thể trong background panel)
   - Style: Có thể match với style pixel art của game

## Bước 2: Setup StoryManager Script

1. **Tạo GameObject cho StoryManager:**
   - `GameObject` → `Create Empty` (đặt tên: `StoryManager`)
   - Thêm component `StoryManager.cs`

2. **Gán References trong Inspector:**
   
   **Dialogue UI:**
   - **Story Panel**: Kéo `StoryPanel` vào
   - **Speaker Name Text**: Kéo `SpeakerNameText` (TextMeshProUGUI) vào
   - **Story Text** (Dialogue): Kéo `StoryText` (TextMeshProUGUI) vào
   - **Speaker Portrait Image**: Kéo `SpeakerPortraitImage` (Image component) vào
   
   **Legacy UI (Optional):**
   - **Story Image**: Kéo `StoryImage` (Image component) vào (nếu có, cho background)
   
   **Buttons:**
   - **Continue Button**: Kéo `ContinueButton` vào
   - **Skip Button**: Kéo `SkipButton` vào (nếu có)

3. **Cấu hình Story Database:**
   - Trong `Story Database` array, thêm các story entries:
     - **Story ID**: ID duy nhất (ví dụ: "Level1_Intro", "Level1_Outro")
     - **Speaker Name**: Tên nhân vật đang nói (ví dụ: "Nhân vật A", "Nhân vật B")
     - **Story Text**: Nội dung lời nói/dialogue
     - **Speaker Portrait**: Sprite portrait của nhân vật đang nói
     - **Story Image**: Background image (optional, cho cảnh không phải dialogue)
     - **Display Time**: Thời gian hiển thị (0 = chờ click)

## Bước 3: Cấu hình GameFlowManager

1. **Mở GameFlowManager trong scene MainMenu:**
   - Tìm GameObject có `GameFlowManager` component

2. **Cấu hình Story Scenes:**
   - **Character Intro Scene**: Đặt là `Assets/Scenes/CharacterIntro.unity` (hoặc chỉ tên: `CharacterIntro`)
   - **Story Scene**: Đặt là `Assets/Scenes/StoryScreen.unity` (hoặc chỉ tên: `StoryScreen`)

3. **Cấu hình Level Definitions:**
   - Với mỗi level trong `Levels` list:
     - **Story Intro ID**: ID của story intro (ví dụ: "Level1_Intro", "Level2_Intro", "Level3_Intro")
     - **Story Outro ID**: ID của story outro (ví dụ: "Level1_Outro", "Level2_Outro", "Level3_Outro")
     - Để trống nếu level đó không có story

## Bước 4: Thêm Story Content

### 4.1: Setup Character Intro Scene

1. **Mở CharacterIntro scene**
2. **Setup UI giống như StoryScreen** (Panel, Text, Image, Buttons)
3. **Tạo GameObject `StoryManager`** và add component `StoryManager.cs`
4. **Gán references** (giống như StoryScreen)
5. **Thêm Story Entry vào Story Database:**
   - Story ID: `"CharacterIntro"` (phải chính xác)
   - Story Text: Nội dung giới thiệu nhân vật
   - Story Image: (optional)

### 4.2: Setup Story Screen Scene (cho Level Intro/Outro)

1. **Mở StoryScreen scene**
2. **Tạo GameObject `StoryManager`** và add component `StoryManager.cs`
3. **Gán references** (Panel, Text, Image, Buttons)

4. **Thêm tất cả 6 Story Entries vào Story Database:**

   **Ví dụ cho Level 1 Intro (Hội thoại 2 nhân vật):**
   
   **Entry 1 - Nhân vật A nói:**
   - Story ID: `Level1_Intro`
   - Speaker Name: `"Nhân vật A"` (hoặc tên bạn muốn)
   - Story Text: `"Chào bạn! Hãy cùng nhau sắp xếp đồ đạc nhé."`
   - Speaker Portrait: Kéo sprite portrait của Nhân vật A vào
   - Display Time: `0` (chờ click)
   
   **Entry 2 - Nhân vật B nói:**
   - Story ID: `Level1_Intro` (cùng ID để tạo sequence)
   - Speaker Name: `"Nhân vật B"`
   - Story Text: `"Được rồi! Tôi sẽ giúp bạn."`
   - Speaker Portrait: Kéo sprite portrait của Nhân vật B vào
   - Display Time: `0`
   
   **Entry 3 - Nhân vật A nói tiếp:**
   - Story ID: `Level1_Intro` (tiếp tục sequence)
   - Speaker Name: `"Nhân vật A"`
   - Story Text: `"Cảm ơn bạn! Hãy bắt đầu thôi."`
   - Speaker Portrait: Kéo sprite portrait của Nhân vật A vào
   - Display Time: `0`

   **Lặp lại tương tự cho:**
   - Level 1 Outro (Story ID: `Level1_Outro`)
   - Level 2 Intro (Story ID: `Level2_Intro`)
   - Level 2 Outro (Story ID: `Level2_Outro`)
   - Level 3 Intro (Story ID: `Level3_Intro`)
   - Level 3 Outro (Story ID: `Level3_Outro`)
   
   **Lưu ý:** Mỗi story có thể có nhiều entries (nhiều câu hội thoại), tất cả cùng Story ID sẽ được hiển thị theo thứ tự.

5. **Nhiều trang story:**
   - Nếu muốn story có nhiều trang, thêm nhiều entries với cùng Story ID
   - StoryManager sẽ tự động hiển thị từng trang theo thứ tự

## Bước 5: Add Scenes vào Build Settings

1. **Mở Build Settings:**
   - `File` → `Build Settings`

2. **Add các scenes:**
   - `CharacterIntro.unity` (nên đặt đầu tiên hoặc sau Boot scene)
   - `MainMenu.unity`
   - `LevelSelect.unity`
   - `StoryScreen.unity`
   - `Level1.unity`, `Level2.unity`, `Level3.unity`

## Bước 6: Test Story System

1. **Test Character Intro:**
   - Chạy game từ scene đầu tiên
   - Character Intro sẽ hiển thị trước MainMenu (chỉ 1 lần)
   - Sau khi xem xong, lần sau sẽ bỏ qua và vào MainMenu trực tiếp

2. **Test Level Story Intro:**
   - Từ MainMenu, chọn level có story intro
   - Story intro sẽ hiển thị trước khi vào gameplay

3. **Test Level Story Outro:**
   - Hoàn thành một level có story outro
   - Nhấn "Done" → Story outro sẽ hiển thị
   - Sau đó chuyển về LevelSelect

## Lưu Ý

- **Story ID phải khớp** giữa `GameFlowManager.LevelDefinition` và `StoryManager.StoryDatabase`
- **Speaker Portrait**: Mỗi entry cần có portrait của nhân vật đang nói
- **Speaker Name**: Tên nhân vật sẽ hiển thị phía trên dialogue text
- **Nhiều entries cùng Story ID**: Tạo sequence hội thoại, mỗi entry là 1 câu nói
- Nếu Story ID không tìm thấy, story sẽ bị skip và chuyển tiếp bình thường
- `Display Time = 0` nghĩa là chờ người chơi click "Continue"
- `Auto Advance = true` + `Display Time > 0` sẽ tự động chuyển trang sau thời gian chỉ định

## Layout Gợi Ý

**Layout với Dialogue Box ở Bottom-Center (Dựa trên layout của bạn):**

```
┌─────────────────────────────────────────────┐
│  [Background Panel]      [Skip Button]      │
│  (Window/Scene View)                        │
│                                             │
│  [Portrait]    ┌─────────────────────┐    │
│                │ Nhân vật A           │    │
│                │ "Lời nói của nhân vật"│    │
│                │                      │    │
│                │    [Continue Button] │    │
│                └─────────────────────┘    │
│                      (Speech Bubble)       │
└─────────────────────────────────────────────┘
```

**Cấu trúc Hierarchy gợi ý:**
```
Canvas
└── StoryPanel
    ├── BackgroundPanel (Right Side - Optional)
    │   └── SkipButton
    ├── SpeakerPortraitImage (Left Side)
    └── DialogueBox (Bottom-Center - Speech Bubble)
        ├── SpeakerNameText
        ├── StoryText
        └── ContinueButton
```

**Lưu ý về Dialogue Box:**
- Dialogue box nên có hình dạng speech bubble với tail chỉ về bên trái (hướng về portrait)
- Có thể dùng Image component với sprite đã vẽ sẵn, hoặc dùng Panel với Image làm background
- Tail của speech bubble có thể là một Image riêng hoặc part của sprite background
- Background Panel bên phải có thể hiển thị scene/illustration (dùng `StoryImage` field nếu muốn)

## Troubleshooting

**Story không hiển thị:**
- Kiểm tra Story ID có khớp không
- Kiểm tra Story Scene có được add vào Build Settings không
- Kiểm tra StoryManager có được gán đúng references không

**Story không chuyển tiếp:**
- Kiểm tra Continue Button có được gán vào StoryManager không
- Kiểm tra `NotifyStoryIntroFinished()` / `NotifyStoryOutroFinished()` có được gọi không

**Story hiển thị nhưng không có text:**
- Kiểm tra Story Text (TextMeshProUGUI) có được gán đúng không
- Kiểm tra Story Database có Story Text được điền không

