# Dopamine Meter 使用说明

## 这是什么

你使用的是一个 **Prefab（可以拖进 Unity 场景的预制对象）**。里面包含：

- **Meter Service**：全游戏唯一的计算器，保存当前值、死亡后的起始值和计时进度。
- **Meter View**：把同一个计算器的数值显示在屏幕上。
- **Meter Game Bridge**：自动检查本项目的对话、暂停、加载和玩家是否可以自由操作。

“全局”指同一轮运行中的游戏。切场景不会清零、不会换计算器。退出程序后的继续游戏需要存档连接，目前提供快照接口，但尚未接入主游戏存档。

## 第一步 把它放进正式游戏

1. 在 Project 面板找到 **Assets → Prefabs → DopamineMeter → DopamineMeterForGame.prefab**。
2. 打开启动游戏时进入的第一个场景，例如 Title。
3. 把这个 Prefab 拖到左侧 **Hierarchy 的空白处**，保存场景。
4. 让它独立放在那里，不要拖到 Player、手机、按钮等对象里面。
5. 其他正式场景不用再放。它自动保留；即使误放第二个，也会删除副本，保留原来的值。

之前说的“根节点”只是第 4 步这种 **Hierarchy 最外层的独立对象**，不是对话节点。

### 为什么后面的场景不用再放

必须区分“Prefab 文件在哪里”和“运行中的对象在哪里”。Prefab 文件现在统一放在 Assets/Prefabs/DopamineMeter。把它拖入场景后，运行游戏时才会产生真正负责计算的对象。

正常从第一个场景启动时：

1. 第一个场景创建一个 Meter。
2. Meter 告诉 Unity：“切换场景时不要删除我。”
3. 进入第二个场景时，原来的 Meter 连同数值、HUD 和计时进度继续存在。
4. 后面的场景只通知这个现有 Meter 加减，不创建新 Meter。例如第一场景当前值是 42，切到第二场景仍然是 42。

因此，从游戏开头正常游玩时，只需在第一场景放一次。**不用反复拖入，但后续道具的加减事件、指定模式的计时开关和正式死亡画面仍要各自接好。** 持久化不会替你猜哪些道具应该加分。

### 开发时直接从后面的场景按 Play 怎么办

如果你在编辑器直接打开第二章并按 Play，这是一次全新的运行，没有经过第一场景，原来的 Meter 根本没有创建。当前实现不会自动凭空生成它。

有两种用法：始终从放有 Meter 的首场景启动；或者在需要单独测试的场景里也放同一个 ForGame Prefab。后一种情况下，单独测试会创建一个新的 Meter，从配置初值开始；正常从首场景切过来时，重复对象会自动删除，仍然使用原来的一个。

不要利用后面场景的重复 Prefab 修改全局数值或覆盖配置，后创建的副本不会替换已经运行的 Meter。真正想改数值请调用互动方法。

停止 Play 或关闭程序会结束本次运行，再次启动不是延续上次的数值。跨程序保存仍需接入存档。

正式死亡画面的接收对象也必须在需要它时仍然存在。如果将 On Death 绑定到首场景里的普通 UI，而该 UI 随切场景被删除，后面就收不到通知；应绑定同样常驻的游戏结束控制器，或让各场景在进入时重新订阅。

默认显示待机，等待拾取设备或开局事件激活。正式场景请使用带 **ForGame** 的版本；基础 `DopamineMeter.prefab` 没有本项目自动对话检查，`MeterDemo` 则只是演示控制台。

## 第二步 在道具或按钮上配置互动

以已经有点击或拾取功能的对象为例：

1. 选中道具或按钮，点击 **Add Component**，添加 **Meter Interaction**。
2. 找到它原有的“拾取成功”事件；UI Button 对应的是 **On Click()**。
3. 点事件下面的 **+**，把这个道具或按钮自身拖进新出现的对象框。
4. 在右边的方法菜单中选择 **MeterInteraction → ActivateMeter()**。

成功拾取后，全局 Meter 开始工作。重复激活不会重置数值。MeterInteraction 只是通知全局计算器，不会创建新计算器。

如果现有道具没有 Inspector 事件，就在已有“拾取成功”的 C# 方法里调用：

```csharp
DopamineMeter.MeterService.Instance.Activate();
```

普通 Collider 本身不会自动执行这些方法，必须连接已有的点击、拾取或触发逻辑。

| 要做什么 | Amount | 选择的方法 |
|---|---:|---|
| 用 AI 后加 5 | 5 | AddPressure() |
| 阅读完成后减 10 | 10 | RecoverPressure() |
| 获得设备后激活 | 不使用 | ActivateMeter() |
| 进入需要计时增长的模式 | 不使用 | EnableTimedGrowth() |
| 离开此模式 | 不使用 | DisableTimedGrowth() |

**Repeatable** 勾选表示可以反复结算。取消表示同一个 Event Id 每局只结算一次。复制一次性道具时，为各个道具设置不同的 Event Id，避免互相去重。死亡重生保留一次性记录，真正的新游戏清空记录。

## 第三步 在 Inspector 开关计时

选中场景里的 **DopamineMeterForGame**，查看 **Meter Service**：

- **Enable Timed Growth** 不勾选（默认）：只有互动事件改变数值。
- 勾选：每累计 20 秒可自由操控时间增长一次。
- 运行时也可勾选或取消；玩法中可以调用前面的 EnableTimedGrowth / DisableTimedGrowth。
- **Player Has Control** 是游戏适配器自动填写的状态，不用手动维护。

点击 Config 指向的 **MeterTuning.asset** 修改数值：

| Inspector 字段 | 默认值 | 含义 |
|---|---:|---|
| Growth Interval Seconds | 20 | 每隔多少秒结算一次 |
| Growth Per Interval | 0.5 | 每次增长多少点；要每 20 秒加 1 就填 1 |
| Initial Value | 50 | 全新一局初始值 |
| Increase After Death | 5 | 每死一次，下次起始值增加多少 |
| Respawn Start Cap | 50 | 重生起始值上限，不能超过 50 |

配置参数在启动时读取，修改后重新 Play。Enable Timed Growth 开关即时生效。

**计时例子：自由操作 12 秒 → 对话 60 秒 → 自由操作 8 秒，此时才加 0.5。** 对话时间不会补算；关闭开关也冻结已累计的时间，再开启时继续。对话中明确触发的加减事件仍然有效。

暂停、标题、加载、失焦、玩家不存在、输入组件禁用、移动被锁定时也不计时。可操控指玩家有自由操作权，即使站着不动也计时，不要求一直按住移动键。

## 第四步 死亡 重生与新游戏

**当前值到 100 仍然死亡。50 仅限制重生起始值，不限制游戏中的当前值。**

例如 Initial Value=35、Increase After Death=5、Respawn Start Cap=50：

**35 开局 → 死后 40 开局 → 再死 45 → 再死 50 → 后续保持 50。**

当前默认初值已经是 50，因此死后仍是 50。若要看到递增，先把 Initial Value 改为 35 或 40。Initial Value 不得大于 Respawn Start Cap。

- 重生按钮调用 **MeterInteraction.RespawnMeter()**：仅死亡后生效，直接恢复运行，不必重新获得设备。
- 真正的新游戏调用 **MeterInteraction.StartNewGame()**：清除死亡累积，回到原始初值和待机。不要把重生按钮接到这个方法。
- 重生和新局都会清空计时余数，并默认关闭计时增长；进入指定模式后再开启。

Meter 的重生只处理数值。玩家位置、动画、恢复输入仍由游戏流程处理。

选中全局 Prefab，找到 **Meter Service → On Death**，把你们的 Game Over 控制器拖到事件里，选择其公开方法。到 100 时它只通知一次。这样数值逻辑与正式死亡画面保持分开。

## 先用 Demo 验证

Unity 菜单 **Tools → Dopamine Meter → Open Demo**，然后 Play。

- **PAIR COMPANION**：激活。
- **TIMED GROWTH**：开关每 20 秒增长。
- **Dialogue / End dialogue**：模拟对话，冻结计时，互动加减仍有效。
- **Field view / Switch scene**：看常驻 HUD，切场景继续同一数值。
- 死亡界面 **RESPAWN**：使用提高后的起始值。
- 控制台 **Reset**：真正的新局。

独立 Demo 不需要真实玩家。正式 ForGame Prefab 自动检查 GameSession、PlayerMover、PlayerInputHandler。其他 Unity 项目没有这些类时，应使用基础 Prefab，并由自己的控制器调用 SetPlayerControl(bool)。

## 程序连接速查

```csharp
var meter = DopamineMeter.MeterService.Instance;
meter.Activate();
meter.DigitalAction("chapter1.query.001"); // 同一次事件不重复计量
meter.SetTimedGrowthEnabled(true);       // 进入特定模式
meter.SetTimedGrowthEnabled(false);      // 离开特定模式
meter.Respawn();                         // 死亡后重生
meter.NewGame();                         // 开始新局
```

快照 Capture / Restore 包含当前值、重生起始值、计时余数、生命状态和一次性事件记录。跨场景保留已实现，跨程序退出的存档连接尚未实现。

图片由内置 image_gen 生成，位于 Assets/DopamineMeter/Art/neural-sensor.png，完整提示词位于同目录 PROMPT.txt。核心检查通过 Tools → Dopamine Meter → Run Core Checks 执行。
