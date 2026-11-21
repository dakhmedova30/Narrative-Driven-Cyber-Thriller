# 咖啡馆交谈场景：视觉草稿 01

分支：feat/Cafe。参考用户给出的构图：玩家位于吧台内侧，来访者站在对面，中间留出人物位置，前景道具营造空间感。

本稿选择雨夜咖啡馆：暖色室内灯光与冷色窗外城市形成对照。背景与透明人物图分别生成，人物仅为匿名成年访客占位，不代表正式角色设定。示例台词同样不是正式剧情。

## 打开

Unity：Tools → Cafe → Open Visual Draft，按 Play。

也可以打开 Assets/Scenes/CafeDraft.unity，或运行 output/cafe/Windows/CafeDraft.exe（保留整个 Windows 文件夹）。

## 预览操作

- Continue：推进占位对话，然后选择一句回应。
- Replay：重新开始。
- Scene Only：隐藏所有草稿界面，查看场景构图。按 H 恢复。
- Visitor：显示或隐藏临时人物。
- Motion：开关非常轻微的呼吸位移。

看纯背景：先关闭 Visitor，再选 Scene Only。也可以直接查看 Assets/Art/Scenes/CafeDraft/cafe-night-background.png。

## 文件和使用

- Assets/Prefabs/Cafe/CafeConversationDraft.prefab：拖入场景即可创建草稿画面；场景须有 Camera，项目现有 GameUI 提供 EventSystem。
- Assets/Art/Scenes/CafeDraft/cafe-night-background.png：无人物、无文字界面的背景。
- Assets/Art/Scenes/CafeDraft/visitor-placeholder.png：独立透明人物，占位用。
- CafeDraft 的 Inspector 可替换 Background、Visitor、Font 和占位台词。

渲染顺序是背景 → 人物 → 从同一背景重绘的吧台前景 → 对话界面。因此人物站在柜台后面，而不是浮在桌面上。换背景且吧台高度不同，需要调整脚本里的前景分界（目前设计画布 y=697）。

本次重点是构图、美术和对话界面的视觉预览，尚未连接正式 Yarn 剧情、角色表情、咖啡制作玩法或存档。没有改变正式游戏入口与场景列表。

美术来源：本次用 OpenAI imagegen 生成原创草稿；只参考用户图片的空间关系，不复用其中角色与 UI。背景采用手绘线条与平涂风格；如最终需要严格匹配低分辨率像素素材，可在构图确认后统一重绘。
