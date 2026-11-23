# 交互演示验证记录

分支：`feat/interaction`。入口：`Assets/Scenes/InteractionDemo.unity`。

## 已验证

- Unity 6000.6.4f1 Windows 演示构建成功。
- 独立程序完成 Terminal → Paper note → Delivery → Exit → 走廊结尾。
- 未完成前置物品时，无法领取后面的物品。
- 取消查看不会完成物品；可以重新打开。
- 一次性物品无法重复领取或重复改变 Meter。
- 查看物品暂停 Meter，并锁住 PlayerMover；关闭后释放自己的锁。
- 开启距离限制时，远处无法交互。
- 游戏暂停菜单打开时，不允许发起新交互。
- 自动化通过 Unity Input System 输入事件验证 E 打开、Backspace 关闭。
- Meter 实际数值为 50 → 55 → 45 → 53。
- 1600×900 与 1280×720 的运行截图检查过布局。
- 使用 Windows 界面工具实际点击终端、确认领取、打开纸条、取消查看，目标和数值反馈正常。

运行自动检查：启动独立程序并传入 `-interaction-smoke`。程序会自动退出，结果写入程序旁 `Captures/smoke-result.txt`；失败则返回非零退出码。截图同样位于 Captures。

## 尚未覆盖的集成

正式章节尚未替换为此演示，也没有新增跨场景存档。Yarn 对话适配组件已添加并参与编译，但独立演示使用物品详情文案，没有用正式 Yarn 剧情做端到端演示。

已有项目的 Unity Version Control 配置与渲染管线提示不属于本次交互代码；以最终构建结果和运行日志为准。
