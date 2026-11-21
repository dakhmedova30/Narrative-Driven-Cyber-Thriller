# 物品交互：怎么运行、怎么放进自己的场景

## 先看成品

Unity 菜单选择 **Tools → Interaction → Open Demo**，然后按 Play。

也可以直接打开 `Assets/Scenes/InteractionDemo.unity`。Windows 独立演示位于 `output/interaction/Windows/InteractionDemo.exe`，分享时需要整个 Windows 文件夹，不能只发送 exe。

演示顺序：Terminal → Paper note → Delivery → Exit。

- 鼠标移到标记上会高亮，点击打开详情。也可以将鼠标移向目标后按 E。
- 详情里按主按钮才会完成交互；Close 或 Backspace 只关闭，不领取、不加分。
- 还没解锁的物品可以点击，底部会说明先要做什么。
- 右侧目标、收集记录和 Meter 随着确认更新：50 → 55 → 45 → 53。
- 最后出现走廊和结束卡。按 Play Again / Replay 可以重新演示。
- Esc 是项目已有的暂停菜单；查看物品时，Backspace 是关闭详情。

## 两种 Prefab，作用不同

所有 Prefab 都在 **Assets → Prefabs → Interaction**。

| Prefab | 放在哪里 | 作用 |
|---|---|---|
| InteractionSystem | 当前游戏场景 Hierarchy 空白处，只放一个 | 显示标记、详情面板，处理鼠标和 E 键 |
| Terminal / PaperNote / DeliveryPackage / ExitDoor | 场景中相应物品的位置 | 保存这个物品的说明、图片、交互规则和完成事件 |

物品 Prefab 是“交互位置与内容”，不包含新的世界物品 Sprite。场景已有手机或包裹图片时，把对应 Prefab 放在图片旁边就可以。也可以直接给自己的物品添加 **Interactable** 组件。

**交互系统与全局 Meter 的区别：** Meter 跨场景保留；InteractionSystem 管理当前场景的物品，随场景一起销毁。因此每个需要交互的场景都放一份 InteractionSystem；它们都会调用同一个全局 Meter。不要把 InteractionSystem 放进 Meter 的持久对象下面。

## 把一个物品做成可以交互

1. 将 `InteractionSystem.prefab` 拖入场景 Hierarchy 的空白处。
2. 确认场景相机标记为 **MainCamera**，或把相机拖入 System 的 **World Camera**。
3. 把 `PaperNote.prefab` 拖入场景，移动到纸条所在的位置。
4. 选中纸条，在 **Interactable** 里编辑以下内容。

| Inspector 字段 | 用途 |
|---|---|
| Display Name | 标记与详情标题，例如“Old receipt” |
| Category | 详情上方的小标题 |
| Description | 玩家读到的正文 |
| Action Label | 确认按钮文字，例如“KEEP NOTE” |
| Artwork | 拖入自己的 PNG 图片，详情会等比例显示 |
| One Shot | 勾选后只会成功完成一次 |
| Hide When Completed | 完成后隐藏整个物品对象；不勾选则保留 DONE 标记 |
| Prerequisite | 把必须先完成的另一个场景物品拖进来；留空则立即可用 |
| Interaction Distance | 0 代表远距离点击；大于 0 需要玩家走到指定水平距离内 |
| On Completed | 确认成功后执行的事件，可以连接显示物体、音效、对话等 |

5. 按 Play。系统默认在 Start 时寻找本场景中的活跃 Interactable；不用手工注册每一个物品。运行后新生成的物品、开始时隐藏而后激活的物品，请在启动前放进 System 的 **Objects** 数组。本版不自动扫描后续动态生成的物品。

开启距离限制后，玩家需要先用现有移动系统走近；点击物品不会自动让角色走过去。一次性状态仅保留到物品被销毁或退出本次运行，并未接入存档。

## 和 Meter 连接

示例物品上有 **Interaction Meter Effect** 组件：Amount 正数增加、负数减少。可以删除这个组件，物品仍能正常查看和领取。该组件不会创建新的 Meter。

在正式游戏首个场景放 `Assets/Prefabs/DopamineMeter/DopamineMeterForGame.prefab`，并按 Meter 使用指南激活它。该物品系统不会自行激活休眠中的 Meter。独立演示由演示脚本自动激活现有 Meter，计时增长默认关闭。

打开详情期间会暂停 Meter，关闭时只移除自己添加的暂停原因，不会解除菜单或其他系统的暂停。按确认时先结束查看，再应用物品数值变化。

## 接一段现有对话（不需要理解“节点”术语）

“节点”可以理解成 Yarn 文件里一段对话的名字，例如 `title: C0_LookAround` 中的 `C0_LookAround`。

给物品添加 **Interaction Dialogue Effect** 组件，把场景中的 DialogueRunner 对象拖进 **Runner**，在 **Conversation Name** 填那段现有对话的名字。组件会自动在物品确认完成后启动对话，不必额外连接 On Completed。确保该对话已经包含在此 Runner 的 Yarn Project 中；不要填整段台词，也不要填文件路径。当前 Yarn 的 StartDialogue 是异步方法，因此这里用适配组件包装，避免在 Inspector 里找不到可选择的方法。

演示场景使用详情文本展示叙事，不依赖正式章节的 Yarn 文件。正式对话运行、暂停、载入期间，系统会禁止发起新的物品交互。

## 给 presentation 的演示话术（约一分钟）

“场景里的物品可以被发现并查看。数字交互会改变右上角的 Meter，而纸质线索提供不同的反馈。完成交互后，目标和收集记录立即更新。连续完成几个动作，出口就会解锁。制作新物品只需要拖入 Prefab 并填写内容。”

## 目前范围

这是一个可运行的交互与展示模块，不是完整背包、任务编辑器或存档系统。右侧目标栏、走廊结尾和 Replay 按钮属于 InteractionDemo；正式场景只使用 InteractionSystem 和物品组件即可。没有加入重生点，也没有修改正式游戏场景。

如果手工修改了演示场景或示例 Prefab，不要随便选择 **Rebuild Demo Assets**，该命令会覆盖生成的示例内容；**Open Demo** 不会覆盖已有场景。

## 后续优先考虑的可见内容

1. 为真正的关键物品补齐独立道具插画和更短的检视文案。
2. 为现有交互接入项目音效：悬停、消息提示、包裹开启、出口解锁。
3. 将一个正式剧情片段放进这个连续流程，让聊天界面、物品查看和场景切换连起来。

这些比现在增加重生点或复杂背包更适合展示；本次先交付上述交互闭环。
