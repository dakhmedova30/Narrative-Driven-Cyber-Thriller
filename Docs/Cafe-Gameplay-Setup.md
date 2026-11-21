# 咖啡馆玩法：怎么展示，怎么改，怎么接 Yarn

## 先运行

Unity 菜单：**Tools → Cafe → Open Gameplay Demo**，然后 Play。

场景：`Assets/Scenes/CafeGameplay.unity`。

独立程序：`output/cafe-gameplay/Windows/CafeGameplay.exe`。分享时发送整个 Windows 文件夹。

原来的 CafeDraft 场景与 Open Visual Draft 菜单仍然保留。

## 展示顺序

1. 客人从右侧走近，讲述近况。点击 Continue；文字没显示完时，第一次点击会显示整句。
2. 客人点一杯拿铁，Yarn 等待咖啡操作完成。
3. GRIND：在浅绿色区域按 Space 或点击操作区。
4. EXTRACT：按住 Space / 鼠标，进度到浅绿色区域时松开。
5. POUR：标记每次到中线时按一次 Space / 点击，总共三拍。
6. 每步显示 Perfect / Good / Miss。完成后点击 Serve Coffee，将结果交给 Yarn。
7. 三步全 Perfect 会出现关于妹妹的额外故事。普通结果可以重做，也可以继续聊；Cancel 也不会卡住剧情。
8. 选 “Show the notification phase”，剧情开启手机通知。看完提示台词后，点击吧台右侧 PHONE。
9. 手机放大，背景向上平移，形成低头看手机的效果。滚轮或拖动列表浏览；点击 VIEW 才算查看新内容并增加 Meter。
10. CLOSE 或 Backspace 收起手机，回到对话。剧情可以再次关闭通知。

演示中的人物、台词与广告内容都是占位素材。它们展示完整流程，不代表最终故事设定。

## 可以拖到自己场景的 Prefab

都在 `Assets/Prefabs/Cafe`：

| Prefab | 用途 | 每个场景放几份 |
|---|---|---|
| CoffeeQTE | 三步咖啡制作面板、输入和结果 | 一份 |
| CafePhone | 桌面手机按钮、通知、可滚动手机界面 | 一份 |

它们属于当前咖啡馆场景，**不跨场景保留**。Meter 依然使用项目全局的那一个。

接入时将两个 Prefab 拖到 Hierarchy 空白处。选 CafePhone，把 CoffeeQTE 拖入 **Coffee** 字段，避免手机遮挡制作。**Stage** 是可选的背景移动组件；不填也能打开手机界面，只是不移动背景。**Dialogue** 是本演示的对话呈现器；填入后，自动通知不会抢占剧情选项。

不要把 `CafeDemoBoot` 搬入正式章节：它专门为独立演示重新初始化 Meter。正式游戏按已有 Meter 生命周期创建和激活。

## 改难度，不改代码

打开 `Assets/CafeGameplay/Config/LatteRecipe.asset`。

| 字段 | 意思 |
|---|---|
| Drink Name | 咖啡名称 |
| Steps | 可增减的制作步骤 |
| Title / Instruction | 标题与操作说明 |
| Input | Timing 点击 / Hold 按住松开 / Rhythm 节拍 |
| Travel Seconds | 指针单程或蓄力全程需要多少秒 |
| Target | Timing / Hold 的目标位置，0–1 |
| Perfect Window / Good Window | Timing/Hold 是目标左右的位置容差；Rhythm 是秒数容差 |
| Timeout | 没操作时最多等多久；节拍步骤至少等待完整节拍时间 |
| Beat Interval / Beats | 节拍间隔与次数 |
| Step Feedback Seconds | 每一步判定后停留多久 |

加大 Perfect Window 或增加 Travel Seconds，会更容易展示成功。Good Window 应不小于 Perfect Window。所有步骤 Perfect 才会使 `$coffee_perfect` 为 true。任何 Miss 则整杯结果为 Miss，但流程仍可继续。

配置与运行时状态分开，试玩不会改写这个资产。创建新配方：Project 右键 → Create → Cafe → Coffee Recipe，再拖到 CoffeeQTE 的 Recipe 字段。

## 手机开关：就在 Inspector

选中场景里的 CafePhone：

| 字段 | 意思 |
|---|---|
| Enable Notifications | 勾选才定时出现通知。默认关闭 |
| Play Notification Sound | 是否响提示音 |
| Auto Open Phone | 来通知后自动低头打开；默认关闭 |
| Sound Volume | 提示音音量 |
| Content | 消息内容与节奏配置资产 |

即使开启 Auto Open Phone，也会等 QTE、暂停菜单、载入、剧情选项结束后再打开。做咖啡期间可以收到提醒，但不能打开手机。通知不默认根据 Meter 高低自动切换，Yarn 决定何时开启。

打开 `Assets/CafeGameplay/Config/PhoneFeed.asset` 修改：

- Notification Interval：每隔多少秒出现一条通知。
- Max Unread：未读上限；达到上限后停止排队，阅读后恢复。
- Look Down Seconds：手机放大与低头过渡时间。
- Notification Sound：可替换项目自己的音效。默认附带简单合成提示音。
- Posts：消息/广告清单。每条填写唯一 ID、来源、标题、正文、Advertisement 勾选和 Meter Increase。

关闭通知会停止新通知并隐藏提醒，但已经收到的内容仍可阅读。通知按列表顺序逐条出现，每条只投递一次，清单用完后不会无限重复响。想展示更密集的后期干扰，可以缩短间隔并添加更多内容。

收到提醒、打开空手机、仅滚动列表都不增加 Meter；点击新内容的 VIEW 才增加。同一条内容在当前手机实例里只计分一次。手机读取的是全局 Meter，不创建第二个数值。若场景没有 Meter，手机本身仍可浏览。

## 连接你们的 Yarn Spinner

给场景中的 **DialogueRunner** 添加 **CafeYarnBridge** 组件，将 Coffee、Phone、Stage 拖入对应字段。不要在同一个 Runner 上放两个 Bridge。

“节点”就是 Yarn 文件里 `title:` 后的一段对话名称。你可以在任何已有的对话里插入这些命令：

```yarn
<<declare $coffee_perfect = false>>
<<declare $coffee_result = "Cancelled">>
<<declare $coffee_perfect_steps = 0>>

<<cafe_notifications false>>
<<cafe_arrive>>
Visitor: Could you make me a coffee?
<<cafe_brew>>
<<if $coffee_perfect>>
    Visitor: That reminds me of someone. Let me tell you more.
<<else>>
    Visitor: Thank you. We can try again later.
<<endif>>
<<cafe_notifications true>>
```

变量只需在整个 Yarn Project 中声明一次。`cafe_brew` 会等待玩家制作并点击 Serve 后才继续；每次开始前会清掉旧结果，避免上次的 Perfect 被误用。

| 命令/变量 | 用途 |
|---|---|
| cafe_arrive | 等待人物入场；Stage 可选 |
| cafe_brew | 等待制作完成或取消 |
| cafe_notifications true / false | 开关通知 |
| cafe_wait_phone | 等待本次通知阶段至少查看一条新内容并收起手机；演示用，非必需 |
| $coffee_perfect | 是否所有步骤完美 |
| $coffee_result | Perfect / Good / Miss / Cancelled |
| $coffee_perfect_steps | 完美步骤数 |

`cafe_wait_phone` 在通知关闭或内容已经耗尽时会直接继续，避免卡死。通知开启且仍有内容时，它会等待玩家查看并收起手机。一般剧情不必使用它；只有明确要求“看完手机再继续”的段落才插入。

完整示例是 `Assets/CafeGameplay/Story/CafeStory.yarn`，有独立的 CafeStory.yarnproject，不改现有 Story.yarnproject。

## 修改职责

- 改剧情与分支：CafeStory.yarn。
- 改 QTE 数值：LatteRecipe.asset。
- 改通知节奏、内容：PhoneFeed.asset 与 CafePhone Inspector。
- 改杯子、进度条、面板视觉：CoffeeQTE；判定逻辑在 CoffeeStepModel。
- 改手机布局：CafePhone；队列与去重在 PhoneInbox。
- 改镜头/人物入场：CafeStage。
- 改对话显示方式：CafeDialoguePresenter；也可以用你们已有的 Presenter，保留 CafeYarnBridge。

所有交互会尊重项目暂停。查看手机时暂停本演示的台词推进与打字，咖啡制作时 Yarn 等待命令完成。

## 范围与后续接入

这里的“低头”是 2D 画面平移与缩放，没有强行改为 3D。背景、人物、咖啡操作、手机和剧情均是独立部分。

当前新增状态没有接入正式存档。重载场景会重新开始咖啡与手机消息记录；正式项目存档需要保存结果变量以外的手机投递/阅读状态。演示沿用项目的自动存档机制，但不要把它当成完整咖啡馆存读档验证。

构建方法是 `CafeGameplayBuilder.Build`。它会重建专用演示场景与示例 Prefab，但不会覆盖已存在的配方/消息配置。手工修改了场景后不要随意重建；日常展示使用 Open Gameplay Demo。
