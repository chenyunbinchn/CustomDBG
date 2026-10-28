# CustomDBG 联机分层与共用代码建议

日期：2026-10-28。性质：基于当前源码的架构建议，不是已批准标准或已实施改造。

目标：先 ENet 本机多开、玩家主机；以后按需要增加 Steam Networking。保持 C# 权威核心可脱离 Unity 的方向，但第一阶段不必单独部署 .NET 服务进程。

## 分层

1. Game.Core（纯 C#）：Id、枚举、规则数据、游戏状态、随机域、规则判定、Action、Hook 与无 Unity 依赖的战斗推进。允许依赖必要的托管序列化包；纯 .NET 不等于只能用 BCL。禁止依赖 Unity/ENet/Steam。
2. Game.Protocol（纯 C#）：显式消息 DTO、编解码、协议版本、消息长度限制；引用 Core 的值类型 Id，不能依赖 Sprite、GameAction 活对象或平台连接句柄。BattleCommand 可作为逻辑载荷，但增加网络封套，不以结构体内存布局为协议。
3. Game.Session（纯 C#）：HostSession、ClientSession、连接到玩家的映射、准备/开始流程、请求去重、权威定序、提交确认/拒绝、同步策略、超时、恢复及断开。引用 Core 和 Protocol，通过传输接口操作网络，不依赖具体 ENet/Steam。
4. Transport：连接/断开、完整字节消息、收发、轮询与句柄释放。先 ENetTransport，以后 SteamTransport。内部连接 Id 与业务玩家 Id 区分。可靠消息通道顺序不替代主机跨客户端统一定序；不同实现的通道语义不保证一一对应。
5. Unity.Client：Presenter、UI、ViewModel、动画、InputSystem、Sprite/Addressables、本地玩家上下文。向 ClientSession 提交意图，读取本地状态/只读视图。
6. Composition/Platform：UnityBoostrap 组装组件、解析 --host/--connect/--player 等开发参数、路径、日志、帧推进；未来独立 .NET 程序可替换该入口。Steam Lobby/邀请为平台房间服务，与传输分离，不放进战斗核心。

这些是逻辑边界，不要求立刻建六个程序集或复制目录。现阶段可先定义依赖方向与少量类，核心去依赖后逐步加 asmdef 验证，避免共享代码反向引用 Assembly-CSharp；不要创建互相依赖的 asmdef。物理文件迁移按项目 namespace==folder path 约束统一规划。

## 当前代码映射

- combat/ActionEntityId、BattleCommand、cards/instance/*Id、enums、gameEffects/Effect、CardDefinition：可作为 Core 共用数据。跨进程仅共用类型，不共享状态对象。
- combat/BattleCommandManager：保留主机权威队列与 CommandId 生成；客户端收到已排序命令不得再次通过当前 Enqueue 生成另一套权威 Id。新增导入有序命令路径或独立接收缓冲，检查重复和间隙。
- systems/BattleCommandApi：拆开可共用的规则预检与执行/调度入口，主机最终校验，客户端预检仅改善交互。Submit 不再由 UI 直接操作 GameState 的权威队列。
- systems/EffectApi、BattlePileApi、BattleApi、action/*、hook/*：规则算法可复用，清除日志等 Unity 依赖，保持唯一共享 ActionQueue 与稳定点原则。
- random/*：复用随机域，不能把同种子当作跨运行时确定性证明；固定实现、内容版本和遍历顺序并验证状态摘要。
- action/ActionExecutor：MonoBehaviour、Coroutine、StartCoroutine 迁到 Unity 适配器外；核心可用显式 Advance/Step 状态机或自有调度。IEnumerator 是 .NET 类型，不必因其名字整套重写，但 WaitUntil/Unity Coroutine 不能保留在可独立运行核心中。
- HandCardChooseAction：已有 ChoiceId、ChoosingPlayer、Candidates、TryChoose/TryCancel，可保留；将 WaitUntil 改显式等待状态，只有主机产生超时决定并按协议传播。等待时仍需泵网络和接收合法选择，不能卡死整个会话。
- gameStates/StateManager 与 GameState：战斗状态与规则管理器保留，MonoBehaviour 注入、Application 路径、录制文件创建、日志移到入口/适配器。按会话创建实例，不能所有房间共享可变随机或队列。
- CardDefinitionManager：当前同时存规则字典和 Sprite 字典，调用 Resources.Load。拆成规则数据加载器和 Unity CardVisualCatalog；可共用同一 JSON/标识，核心忽略视觉资源加载。
- recording：记录模型、查询和 sinks 可复用/去日志依赖，BehaviorRecorderOverlay 留 Unity。诊断字符串、时间戳、可失败日志不能充当权威同步协议；建立显式业务消息。已有记录字段不代表有完整状态快照实现。
- ui/*：视图、意图和大部分 ViewModel 转换保留客户端。BattlePresenter 读 StateManager、直接 Submit、以 IsBattleStable 判断请求完成的方式需调整：从 ClientSession 获取状态、发请求，以 requestId 的确认/拒绝/应用状态释放等待；远端玩家结果也触发刷新。等待未完成时全局 executor idle 不能证明该请求已处理。
- tools/assert/MyAssert：只在 UNITY_EDITOR/DEVELOPMENT_BUILD 生效且依赖 Unity Debug；纯 .NET 的规则/网络输入检查必须使用始终执行的校验，不能靠该断言保障。
- mono/UnityBoostrap：只负责启动和调度。当前 Q 直接 EnterBattle、S 直接 DrawCards、W 写死 player0；联机流程需要改主机会话控制/受控调试请求，纯客户端不能直接修改权威状态；playerNum 由房间确定，local player 由握手分配。主机决定初始种子，当前 GameState.Init 的 41u 仅调试值。

## 数据流与同步策略

主机进程：一个权威 HostSession + 一个本地 ClientSession + UI。远端进程：ClientSession + UI。主机本地输入走同一权限、校验、排队入口，可用进程内传递，不必绕网卡。HostSession 是业务角色，不是 ENet 的 Host 句柄；ENet 客户端本身也可能持有该库的 Host 对象。

网络请求封套至少包括请求 Id、会话/对局世代、命令载荷。协议版本可在握手确定，不要求每包重复携带。连接在握手后绑定 Player，不接受用户随意控制另一个玩家；权威 CommandId 与客户端 requestId 是不同概念。主机对每连接检查准入、长度、速率和字段，再在出队时做依赖当前状态的规则检查。命令排队不能因结算忙被丢弃，但入口需有容量上限并明确拒绝/断开，不能无限堆积。

A. 沿用旧设计的建议起点：主机排序，所有端运行相同 Core。主机分配 CommandId 后广播权威命令流，各端只按该顺序模拟；主机自身也仅消费一次流。需要明确广播入口与验证时机：可沿用全端在稳定点按相同规则处理已定序命令（含拒绝），主机对请求者回报终态；客户端的执行结论不是权威。也可主机先结算再广播接受结果，但遇到等待选择的命令必须先传播其起始/等待状态，不能只在完整结算后广播，避免客户端永远无法收到选择提示。不要混用这两种时序。

严格串行普通命令、等待选择时旁路处理选择的语义已有实现。网络协议必须能让该选择在各端挂起点被消费；不能把选择放在同一无法越过的普通命令 FIFO 尾端。对局开始需主机统一玩家槽、卡牌初态、种子和内容版本；对局中稳定点可核对摘要。断线恢复需设计快照与流序号衔接；首版可以明确主机退出结束对局、运行中不支持重连，不能称自动具备恢复。

B. 另一种方案：仅主机运行模拟，客户端应用权威结果/快照。规则在主机复用，客户端用协议值和展示副本；预检可调用可用的共享规则。这样避免全端确定性要求，但需完整结果 DTO/客户端应用器，并非 ENet 或分层强制要求。初期每条命令稳定点发送快照可简化状态同步，但等待选牌提示需要单独发送。若要流畅动作动画还需结构化事件。

本报告不擅自废弃旧规则中“各端确定性模拟”的方向。建议第一版沿用 A，若产品要改 B 应单独明确取舍与更新标准。共享核心不等于所有客户端被允许发权威决定，代码复用与权限是不同概念。

## 迁移步骤

1. 先把 Presenter 的写入改 ClientSession/IBattleCommandSender，把本地身份改 LocalPlayerContext；提供进程内 HostSession 路径，保持现有单进程玩法。
2. 拆 Sprite、路径、日志/断言与核心执行调度，保证核心可独立运行。把等待选牌变为显式状态，验证规则不受 UI 动画驱动。
3. 定协议封套、编解码、握手、requestId 和权威顺序；确定 A 的广播/验证时序，复用旧模拟规则，不先实现两种同步架构。
4. 接 ENet 本机两进程：主机和纯客户端，验证双方出牌、同时提交定序、选牌、拒绝、状态摘要。先不实现 Steam，不默认支持重连或迁主机。
5. 按需增加 Steam 房间服务和传输适配，保留相同 DTO 与业务会话。检验所有消息模式，不默认平台通道等价。

验证重点：主机自己和远端提交同一路径；跨连接统一顺序；重试不重复扣能量；客户端不能伪造其他 Player；初态和随机消费一致；选择等待不堵塞网络；未知版本和格式损坏有界处理；UI 从权威流刷新而不是猜测命令结束。没有进行代码修改或运行测试。
