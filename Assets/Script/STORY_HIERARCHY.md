# Sơ Đồ Hierarchy - Story System

## Cấu Trúc Hierarchy Chi Tiết

```
Canvas (Screen Space - Overlay hoặc Screen Space - Camera)
│
└── StoryPanel (GameObject hoặc Panel)
    │   Component: RectTransform
    │   Component: Image (Optional - Background)
    │
    ├── BackgroundPanel (Panel hoặc Image - Right Side)
    │   │   Component: RectTransform
    │   │   Component: Image (Background Image/Sprite)
    │   │   Anchor: Top-Right hoặc Center-Right
    │   │   Size: Chiếm phần bên phải màn hình
    │   │
    │   └── SkipButton (Button)
    │       │   Component: RectTransform
    │       │   Component: Image (Button Background)
    │       │   Component: Button
    │       │   Anchor: Top-Right của BackgroundPanel
    │       │
    │       └── Text (TextMeshProUGUI)
    │           Component: TextMeshProUGUI
    │           Text: "Skip" hoặc "Skip>"
    │
    ├── SpeakerPortraitImage (Image - Left Side)
    │   Component: RectTransform
    │   Component: Image
    │   Anchor: Left, Bottom (gần DialogueBox)
    │   Size: 200x200 hoặc tùy chỉnh
    │   Preserve Aspect: ✓
    │   Sprite: Sẽ được gán từ StoryManager
    │
    └── DialogueBox (Panel - Bottom-Center, Speech Bubble Style)
        │   Component: RectTransform
        │   Component: Image (Speech Bubble Background)
        │   Anchor: Bottom-Center
        │   Size: Chiều rộng ~70-80% màn hình, chiều cao tùy chỉnh
        │   Sprite: Speech bubble với tail chỉ về bên trái
        │
        ├── SpeakerNameText (TextMeshProUGUI)
        │   Component: RectTransform
        │   Component: TextMeshProUGUI
        │   Anchor: Top-Left của DialogueBox
        │   Position: Top, với padding
        │   Font Size: 28-32
        │   Alignment: Left hoặc Center
        │   Text: Sẽ được gán từ StoryManager
        │
        ├── StoryText (TextMeshProUGUI)
        │   Component: RectTransform
        │   Component: TextMeshProUGUI
        │   Anchor: Stretch (fill DialogueBox)
        │   Position: Dưới SpeakerNameText, với padding
        │   Font Size: 24-28
        │   Alignment: Left hoặc Center
        │   Wrap Text: ✓
        │   Text: Sẽ được gán từ StoryManager
        │
        └── ContinueButton (Button)
            │   Component: RectTransform
            │   Component: Image (Button Background)
            │   Component: Button
            │   Anchor: Bottom-Right của DialogueBox
            │   Position: Bottom-Right, với margin
            │
            └── Text (TextMeshProUGUI)
                Component: TextMeshProUGUI
                Text: "Continue" hoặc "Continue>"
```

## Sơ Đồ Visual Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Canvas                                                       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ StoryPanel                                           │   │
│  │                                                      │   │
│  │  ┌──────────────────┐              ┌─────────────┐ │   │
│  │  │ BackgroundPanel  │              │             │ │   │
│  │  │ (Right Side)     │              │             │ │   │
│  │  │                  │              │             │ │   │
│  │  │  ┌───────────┐  │              │             │ │   │
│  │  │  │SkipButton │  │              │             │ │   │
│  │  │  └───────────┘  │              │             │ │   │
│  │  │                  │              │             │ │   │
│  │  │                  │              │             │ │   │
│  │  └──────────────────┘              │             │ │   │
│  │                                      │             │ │   │
│  │  ┌──────────┐      ┌─────────────────────────────┐ │   │
│  │  │ Portrait │      │ DialogueBox                 │ │   │
│  │  │ Image    │      │ (Speech Bubble)             │ │   │
│  │  │          │      │                             │ │   │
│  │  │          │      │  ┌───────────────────────┐ │ │   │
│  │  │          │      │  │ SpeakerNameText       │ │ │   │
│  │  │          │      │  │ "Nhân vật A"          │ │ │   │
│  │  │          │      │  └───────────────────────┘ │ │   │
│  │  │          │      │                             │ │   │
│  │  │          │      │  ┌───────────────────────┐ │ │   │
│  │  │          │      │  │ StoryText             │ │ │   │
│  │  │          │      │  │ "Lời nói của nhân vật"│ │ │   │
│  │  │          │      │  └───────────────────────┘ │ │   │
│  │  │          │      │                             │ │   │
│  │  │          │      │          ┌──────────────┐   │ │   │
│  │  │          │      │          │ContinueButton│   │ │   │
│  │  │          │      │          └──────────────┘   │ │   │
│  │  │          │      └─────────────────────────────┘ │   │
│  │  └──────────┘                                       │   │
│  │                                                      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Component Mapping với StoryManager

Khi gán vào StoryManager Inspector:

```
StoryManager Component:
│
├── Story Panel
│   └── Kéo: StoryPanel GameObject
│
├── Dialogue UI
│   ├── Speaker Name Text
│   │   └── Kéo: SpeakerNameText (TextMeshProUGUI)
│   │
│   ├── Story Text
│   │   └── Kéo: StoryText (TextMeshProUGUI)
│   │
│   └── Speaker Portrait Image
│       └── Kéo: SpeakerPortraitImage (Image)
│
├── Legacy UI (Optional)
│   └── Story Image
│       └── Kéo: BackgroundPanel (Image) hoặc để null
│
└── Buttons
    ├── Continue Button
    │   └── Kéo: ContinueButton (Button)
    │
    └── Skip Button
        └── Kéo: SkipButton (Button)
```

## Setup Steps

### Bước 1: Tạo Canvas và StoryPanel
1. Tạo Canvas (nếu chưa có)
2. Tạo StoryPanel làm child của Canvas

### Bước 2: Tạo BackgroundPanel
1. Tạo Panel hoặc Image trong StoryPanel
2. Đặt tên: `BackgroundPanel`
3. Anchor: Top-Right hoặc Center-Right
4. Tạo SkipButton trong BackgroundPanel

### Bước 3: Tạo SpeakerPortraitImage
1. Tạo Image trong StoryPanel
2. Đặt tên: `SpeakerPortraitImage`
3. Anchor: Left, Bottom
4. Position: Gần DialogueBox (bên trái)

### Bước 4: Tạo DialogueBox
1. Tạo Panel trong StoryPanel
2. Đặt tên: `DialogueBox`
3. Anchor: Bottom-Center
4. Thêm Image component với sprite speech bubble
5. Tạo 3 children:
   - SpeakerNameText (TextMeshProUGUI)
   - StoryText (TextMeshProUGUI)
   - ContinueButton (Button)

### Bước 5: Gán vào StoryManager
1. Tạo GameObject `StoryManager`
2. Add component `StoryManager.cs`
3. Gán tất cả references theo sơ đồ trên

## Lưu Ý

- **DialogueBox** nên có Image component với sprite speech bubble (có tail chỉ về bên trái)
- **SpeakerPortraitImage** sẽ tự động thay đổi sprite theo người nói
- **BackgroundPanel** có thể dùng `StoryImage` field nếu muốn thay đổi background theo story
- Tất cả Text nên dùng **TextMeshPro** (không phải Text cũ)
- Đảm bảo tất cả elements có RectTransform và được anchor đúng vị trí

