# Chapter 0 原场景交互测试

## 这一版做什么

从原来的 `SampleScene` 复制场景，完整复制 Chapter 0 的 Yarn 剧情。保留原背景、普通对话框、MHM / Jaycord 聊天界面。只在回到 2027 房间后加入可跳过的物品探索。旧 InteractionDemo、原 Chapter 0 和原场景不改动。

房间里可以点日历、电脑、床、门。日历放大房间原画中的日历区域；物品的文字仍由原来的 Yarn 对话框显示。读完回到探索；右上角 Continue story 随时进入原来的“查看电脑 / 躺一会儿”选项。新增台词是演示草稿，可直接改 Yarn。

## 立即 Playtest

1. 确认 Git 分支是 `feat/interaction`，等待 Unity 编译完成。
2. 在 Project 打开 `Assets/Scenes/Chapter0_InteractionTest.unity`，按 Play。
3. 按空格读完回家两句台词（文字正在显示时先补全文字，再按一次继续），然后点击房间物品。日历放大时继续读台词，读完自动收起。
4. 再点一次日历，可以看到第二次查看的不同台词。
5. 点右上角 Continue story，出现原来的两个选项；选择 Lie down 后继续到敲门剧情。
6. 停止并重新 Play，这次不点任何物品，直接 Continue story，确认可以跳过探索。

完整流程用 `Assets/Scenes/Chapter0_InteractionFull.unity`，从 Start 开始。快速测试场景只从 C0_GoHome 开始，前面聊天分支变量使用默认值。

菜单 `Tools > Interaction > Open Chapter 0 Room Test` 可以直接打开快速场景。不需要先在空场景拖 prefab。测试场景不加入正式 Build Settings，也不替换游戏主线入口。

## Yarn 如何暂停和恢复

不是 Stop 掉对话，也不另开一个 DialogueRunner。`<<room_explore bedroom>>` 是一个等待玩家选择的指令：指令没有结束时，Yarn 不会继续下一行。点击物品后，把物品 ID 写入 `$room_action`，结束等待；同一个 Yarn 流程根据变量播放物品台词，然后跳回探索节点。点击继续则跳到原来的故事节点。

```yarn
title: MyRoomExplore
tags: face
---
<<room_explore bedroom>>
<<if $room_action == "calendar">>
    <<room_show calendar>>
    Skye: A calendar.
    <<room_hide>>
    <<jump MyRoomExplore>>
<<else>>
    <<jump MyNextStoryNode>>
<<endif>>
===
```

在同一个 Yarn Project 中声明一次 `<<declare $room_action = "">>`。需要记忆的物品再加布尔变量，例如 `$room_calendar_seen`；“等玩家点东西”本身不需要布尔变量。`tags: face` 让原 ChatAppRouter 使用普通对话框。

## 以后怎么复用

- Prefab：`Assets/Prefabs/Interaction/Chapter0/RoomExploration.prefab`。每个需要探索的场景放一个，属于场景交互层，不是全局 Meter。整个流程仍使用该场景原本的唯一 DialogueRunner。
- 根节点 `RoomExploration`：把场景现有 DialogueRunner 拖到 Runner；Room Image 指向当前背景；Items 数组放该房间的物品组件。Runner 没填会自动寻找，但正式场景推荐明确拖入。
- 子物品 `RoomInteractable`：Item Id 对应 Yarn 里的字符串；Room Id 对应 `room_explore` 的参数；Display Name 是小标签；Region 是原背景上的点击区域。
- Region 使用 0–1 坐标，原点是图片左下角。X/Y 是左下角位置，W/H 是宽高。例如日历大约 `(0.328, 0.639, 0.146, 0.278)`。区域跟随原背景的铺满裁剪规则缩放。
- 要放大物品：设置 Inspection Image 和 Inspection Crop，然后 Yarn 调用 `room_show 物品ID`、播放台词、`room_hide`。整张物品图的 Crop 是 `(0,0,1,1)`；不设置图片就只显示台词。
- 新增物品：复制一个子物品，修改唯一 Item Id、区域和标签，加入 Items；在 Yarn 的 if/elseif 增加该 ID 的台词。代码不用为每件物品加一个类。
- 多个房间背景比例不同时，应使用各自场景配置或扩展为房间配置资产；当前 prefab 对应一张背景的坐标空间。每个 DialogueRunner 只挂一套此命令处理器。

独立剧情位于 `Assets/Chapter0Interaction/Story/Chapter0.yarn` 与 `Chapter0Interaction.yarnproject`，放在原 Story 项目目录之外，避免同名节点重复编译。正式接入时将探索节点和入口改动合并回主剧情，再把 prefab 放进正式场景；不必把两份 Chapter 0 都塞进正式 Yarn Project。

## 注意边界

这次完整复制原章，但不重写原章已有的相机/show/hide 等演出占位指令。探索暂停的是 Yarn 执行，不是全局时间；菜单暂停时不接受物品选择。当前保存系统按节点保存，载入探索节点会重新等待选择，物品是否看过由 Yarn 变量恢复；不承诺恢复到台词中的每个字符。

自动验证仅在独立程序加 `-room-smoke` 时运行，普通 Play 不会自动点选。生成器 `Chapter0InteractionBuilder.Build` 用于开发构建；不要把重新生成当成日常打开方式，因为它会重设测试场景的交互配置。

## 本次验证结果

2026-11-23：Unity 6000.6.4f1 编译及 Windows 构建成功；本机应用程序控制阻止新 EXE 启动，所以在已安装的 Unity Editor 中实际进入 Play 模式完成验证，未修改系统安全设置。

33 项断言通过，覆盖实际 EventSystem 射线命中、四件物品、暂停菜单阻止点击、日历放大及收起、原 LinePresenter 显示台词、重复查看变量、恢复原选项、接回敲门剧情、不查看任何物品直接跳过。已检查房间、日历放大和原选项截图。完整章节的前后所有分支没有逐一重跑。

日志：`output/chapter0-interaction/editor-playtest.log`。截图与结果：`output/chapter0-interaction/Captures/`。这些构建产物不提交 Git。原 SampleScene、Chapter 0 Yarn、DialogueSystem prefab 和旧 InteractionDemo 没有修改。
