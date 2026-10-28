# 服务端拆分与网络传输选型分析

日期：2026-10-28。性质：研究建议，尚未确立新标准或实施架构改造。

## 结论

本项目可使用 ENet 部署云端权威服务，但当前卡牌玩法并不突出需要其低延迟 UDP 能力。首选评估纯 .NET 服务端 + WSS 消息通信；如果明确需要可靠 UDP，C# 技术栈优先比较 LiteNetLib；已有 ENet 经验或跨语言原生生态需求时选 ENet 也合理。没有服务器规格、目标客户端平台和并发实测，不能承诺单机承载数量。

## 项目证据与架构变化

- CLAUDE.md 与 Documentation/rules/260717-rule-multiplayer-battle-model.txt 记录的是玩家主机 listen server。用户此次提出独立云端服务端，本报告以新方向评估；没有改写旧规则。
- Assets/Scripts/systems/BattleCommandApi.cs 已有 Submit/Pump、规则校验和串行结算入口，适合在网络接入层后复用。
- Assets/Scripts/action/ActionExecutor.cs 持有 MonoBehaviour，通过 StartCoroutine 执行；HandCardChooseAction.cs 用 WaitUntil 等待选择。
- Assets/Scripts/gameStates/persistant/GameState.cs 初始化接收 MonoBehaviour，访问 Application.persistentDataPath，并初始化随机域、命令管理器、执行器。
- Packages/manifest.json 中包含 multiplayer.center；这本身不等于已接入游戏传输或同步框架。

因此，网络库并非主要拆分障碍。纯 .NET 方案需要抽出战斗核心、调度器、日志和数据加载适配，等待选牌改为显式挂起状态及超时。IEnumerator 属于 .NET，本身不是 Unity 专属；MonoBehaviour、Coroutine、WaitUntil 才是这里的具体依赖。Unity Dedicated Server 可作为复用现有运行方式的过渡路径，资源成本需要实测。

## ENet 的收益与代价

ENet 提供 UDP 上的可选可靠传输、顺序、独立通道、连接管理和分片重组。命令必须使用可靠有序模式；不可靠或只保留最新包的模式不适合出牌、选择与结算事件。独立通道之间没有全局顺序保证，战斗顺序仍需服务端按房间分配序号。可靠 UDP 在丢包时仍会等待重传，不等于没有队头阻塞。

ENet 是传输层，仍需实现房间、身份认证、会话恢复、命令幂等、状态快照和权限过滤。不能把可靠送达等同于业务执行恰好一次，也不能把普通 ENet 连接视为已认证加密连接。加密方案需单独设计，不能给 UDP 套用 TCP 的 TLS 配置。

C# 接入通常涉及原生库部署与平台架构匹配。尤其 nxrighthere/ENet-CSharp 明确声明采用修改过的协议，不能默认与原版 ENet 或其他绑定互通；两端必须锁定兼容实现与版本。其现成包的平台覆盖也应逐项核验。

## 备选方案

| 方案 | 适用性 | 主要取舍 |
|---|---|---|
| ASP.NET Core + WSS | 当前优先评估 | 可靠有序、TLS 与常规运维方便；自行定义应用消息、恢复与背压；TCP 丢包时有队头阻塞 |
| ASP.NET Core SignalR | 希望减少连接和消息分发代码时评估 | 提供 Hub 等抽象；Unity IL2CPP、裁剪与目标平台需先验证；连接恢复不等于对局恢复 |
| LiteNetLib | C# 栈需要可靠 UDP 时优先比较 | 支持 Unity/.NET、可靠有序与多通道；仍须自行建设游戏会话与安全机制 |
| ENet | 熟悉 ENet 或有原生跨语言需求 | 精简成熟的传输能力；C# 原生依赖和实现兼容性需要管理 |
| 原生 TCP + TLS | 有成熟自定义协议基础时 | 需处理帧边界、缓冲区及连接生命周期；对本项目未见比 WSS 明显的收益 |
| Mirror / NGO 等 Unity 高层框架 | 服务端保留 Unity，且需要对象复制/RPC 时 | 与 ENet 不属于同一抽象层；当前纯命令/数据战斗未必需要完整对象同步框架 |

LiteNetLib 当前 master 文档标明可能不稳定，接入应固定经验证的发布版本。SignalR 的自动重连需要配置，首次连接失败重试以及恢复对局需另行处理。若将来支持 Unity Web，不能直接沿用普通原生 UDP/TCP Socket 方案，需浏览器可用传输与客户端适配。

## 建议结构与同步策略

Unity Client：输入、表现、本地缓存。
Shared：协议 DTO、Id、可复用战斗规则与数据定义。
Server：连接认证 -> 房间路由 -> 有界命令入口 -> 房间串行结算 -> 结果/事件发送；负责超时与存档。

每个房间独立状态、队列、随机域与序号。服务端不等待客户端动画。网络线程不直接并发修改战斗状态。命令携带会话、房间、客户端请求 Id 等信息，玩家身份由认证会话绑定，不能信任包内自报 Player。服务端为已接受结果分配房间内顺序；请求 Id 用于重试去重，状态版本用于增量与快照衔接。

初期建议服务端执行权威模拟，客户端接收获准可见的结果与快照。若继续使用命令广播、各端模拟，必须验证 Unity 与 .NET 的确定性、规则版本、随机实现和恢复机制；相同种子不足以证明跨运行时相同结果。对抗模式还需避免把隐藏手牌、牌库或可预测隐藏结果的种子泄露给客户端。

## 阿里云轻量部署

官方防火墙文档支持 TCP/UDP，因此部署 ENet 没有产品层面的协议冲突。云防火墙与操作系统防火墙均需放行实际端口，服务监听可对外访问的地址；WSS 通常走 TCP 443，ENet 使用指定 UDP 端口。若有容器还需映射对应协议。公网服务器接受客户端主动连接通常无需做玩家之间的 NAT 打洞。

初期可在一台 Linux 实例内运行一个服务进程管理多个房间，避免默认每局启动一份 Unity 进程。套餐的 CPU、内存、公网带宽、流量和地域需具体核查。容量应通过真实结算、快照、日志和断线恢复压测决定；共享单进程也意味着故障可能影响多个房间，需保存恢复所需数据。

建议验证：目标平台连接与序列化；真实玩家网络的 RTT/丢包表现；断线后快照恢复；重复命令不重复扣费；并发房间隔离；内存、出网和结算耗时。当前为分析任务，未安装库、改代码或进行容量测试。

## 官方参考（2026-10-28 查阅）

- ENet 设计：https://github.com/lsalzman/enet/blob/master/docs/design.dox
- ENet-CSharp 平台、原生依赖及修改协议声明：https://github.com/nxrighthere/ENet-CSharp
- LiteNetLib 功能与 Unity 接入说明：https://github.com/RevenantX/LiteNetLib
- SignalR .NET 客户端与重连：https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-10.0
- SignalR 消息协议：https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-10.0
- 阿里云轻量防火墙：https://www.alibabacloud.com/help/zh/simple-application-server/user-guide/manage-the-firewall-of-a-server
- Unity Web 网络限制：https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-networking.html
