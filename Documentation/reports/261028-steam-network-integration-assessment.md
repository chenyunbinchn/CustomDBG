# Steam 联机接入与分层评估

日期：2026-10-28。研究记录，未实施代码改造。

## 本地参考证据

核查 D:\Decompiled Projects\Slay the Spire 2\src\Core\Multiplayer 下的本地反编译版本，不能据此保证与 Steam 当前发布版本完全一致。

- Transport/Steam/SteamHost.cs：SteamHost : NetHost；StartHost 调用 SteamMatchmaking.CreateLobby（FriendsOnly）及 SteamNetworkingSockets.CreateListenSocketP2P；Update 轮询消息。
- Transport/Steam/SteamClient.cs：使用 SteamNetworkingSockets.SendMessageToConnection 等连接接口。
- Transport/NetHost.cs、NetClient.cs 定义消息发送、连接更新、断开等抽象。
- Transport/ENet 同时存在 ENetHost、ENetClient 等实现。

结论：本地参考版本确实集成 Steam 房间与网络传输，并采用可替换传输实现。未推断 ENet 实现对普通用户具体开放方式，也未据此推断其全部权威同步策略。

## 接入架构

保留战斗核心与权威命令处理。在外围分别建立房间发现/邀请适配和消息传输适配：SteamLobbyService 负责 Lobby、成员、邀请与主机发现；SteamTransport 负责 P2P 建连、消息与连接状态。ENetTransport 作为另一种实现。SteamId 映射到项目内部连接 Id 和玩家槽位，不作为 BattleCommand.Player 的直接替代。Steam Lobby 成员关系不等于游戏连接已建立，Lobby owner 改变也不等于权威状态自动迁移。

Steam 的 P2P 描述通信方式，不要求所有玩家两两连接。可以由一个玩家主机接收其他人的命令并运行权威逻辑。Steam Relay 转发流量，不执行战斗规则。小型玩家主机项目可以不部署自有战斗服务器；Steam 房间与传输也不能替代游戏存档和恢复机制。

## 额外工作

1. 选择维护中的 C# Steamworks 绑定，验证 Unity/IL2CPP、目标 OS 与原生 DLL；集成初始化、AppID、Steam 客户端、回调泵与退出清理。
2. 实现创建/加入/离开 Lobby、人数和可见性、协议及内容版本等元数据、好友邀请、游戏内接受邀请和通过启动参数加入的流程。
3. 用 SteamNetworkingSockets 的 P2P API 建立和接收连接、初始化 Relay 接入、处理异步路由及状态、消息收发和资源释放。明确发送模式映射，不能假设 Steam 与 ENet 的通道语义一一对应。
4. 将远端平台身份绑定到房间成员与内部玩家，校验准入、请求归属及版本。身份能力不能替代战斗合法性检查。
5. 继续实现命令排序、去重、初始化快照、就绪握手、超时、掉线恢复和主机退出策略。只有第一版明确不迁移主机，才能省略迁移实现。
6. 在不同电脑、不同账号和网络条件下验证邀请、路由、丢包、退出与恢复；同一 Steam 账号多开不是完整的多玩家测试替代。

Steamworks 开发和正式发布需要相应 AppID/权限及测试用户访问配置；SDK 的示例 AppID 可用于受限开发测试，不是产品发布身份。接入原型不必等到商店正式发布。具体权限和分发方案按官方文档及应用后台确认。

## ENet 与 Steam 的关系

只增加 Steam Lobby 来发布普通 IP 地址，并不会自动让 ENet 流量使用 SDR。普通 ENet UDP 不会因使用 SteamId 或 Steam API 初始化而获得 Steam 转发。对于当前学习项目，推荐 ENet 和 SteamNetworkingSockets 是两种传输实现，复用业务协议；避免叠加两套可靠传输。若最终目标锁定 Steam 好友联机，可优先实现 Steam 原型，ENet 继续作为课程及局域网练习。

GameNetworkingSockets 的开源库不等同于拥有 Steam 平台身份、Lobby 和 Valve SDR 服务权限，不能简单下载开源代码就假设获得整套 Steam 服务。

## 成本判断

分层让大部分战斗与消息协议可复用，降低改动范围；完整 Steam 好友联机仍为中等集成工作。只跑通两个用户收发消息通常比完整房间、邀请、恢复与版本兼容简单。若网络业务直接依赖 ENet Peer、IP、通道与资源生命周期，迁移成本明显提高。抽象应覆盖真实差异，保持可靠发送、连接状态、逻辑端点、消息事件等必要能力，无需提前建设通用多平台框架。

## 官方资料

- https://partner.steamgames.com/doc/features/multiplayer/networking
- https://partner.steamgames.com/doc/features/multiplayer/matchmaking
- https://partner.steamgames.com/doc/features/multiplayer/steamdatagramrelay
- https://partner.steamgames.com/doc/sdk/api
- https://github.com/ValveSoftware/GameNetworkingSockets

## 补充：实际选择入口核查

继续核查本地参考版本：

- NetHostGameService.cs:51 的 StartENetHost 创建 ENetHost；:56 的 StartSteamHost 创建 SteamHost。二者都赋值给同一个 NetHost 字段，并非串联调用。
- Nodes/Screens/MainMenu/NMultiplayerHostSubmenu.cs 的 StartHostAsync：SteamInitializer.Initialized 且没有 fastmp 参数时选择 Steam，否则选择 ENet（端口 33771）。
- Nodes/Screens/MainMenu/NJoinFriendScreen.cs 的 FastMpJoin 用 ENetClientConnectionInitializer 连接 127.0.0.1:33771；表明至少有本机快速联机/测试用途，不能从该入口推断已提供完整玩家 LAN 产品功能。
- Transport/ENet/ENetHost.cs 使用 Godot.ENetConnection.CreateHostBound 和 ENetPacketPeer；这是经 Godot 暴露的 ENet 接口，不是 nxrighthere/ENet-CSharp。
- Transport/Steam/SteamClient.cs 的 SendMessageToHost 直接调用 SteamNetworkingSockets.SendMessageToConnection，没有经过 ENet。

因此，该版本共用上层游戏消息与服务，Steam 与 ENet 是可替换传输。正常 Steam 初始化成功且不带 fastmp 的创建房间入口走 Steam；不是所有联机都通过 ENet 再交 Steam 转发。上述分支是启动选择，不应描述为连接失败时自动无缝回退。

一般而言也可组合 Steam Lobby + 普通 ENet/IP 连接，但这样 Steam 仅提供发现与平台功能，ENet 流量不会自动获得 SDR；需自行解决直连公网可达性。对当前项目建议把两者作为并列适配器。若确需将自定义 UDP 协议接入其他隧道/底层，需要专门适配，超出已核查参考实现，不作为初学阶段推荐。

## 补充：仅 Steam 好友联机的学习路线

用户进一步限定最终仅做 Steam 好友联机。该前提下可直接采用 Steam Lobby + SteamNetworkingSockets，无需先接入 ENet；仍保留消息协议和战斗核心与平台类型的边界，但不必提前实现多套传输。

Steam 仍适合学习应用层游戏网络：异步消息、序列化、身份绑定、权威校验、排序、去重、快照、恢复及网络异常。Socket、重传与拥塞等底层机制被封装，ENet 也有类似封装；可通过独立 TCP/UDP 小练习补足，不要求将底层实验全部加入 CustomDBG。课程的 ENet API 无法直接复制，网络概念可以迁移。

再次核查本地参考源码：Nodes/Debug/Multiplayer/NMultiplayerTest.cs:272 明确按 steam 参数选择 ENet 或 Steam；NJoinFriendScreen.FastMpJoin 通过 clientId 指定身份并连接本机 127.0.0.1；主机菜单用 fastmp 参数选择 ENet。能确认 ENet 提供独立于 Steam 的开发/快速联机路径。可合理推断这种路径便于本机多进程调试、减少多账号及平台依赖、排查业务与平台问题；这属于工程推断，未发现开发者解释其全部历史动机的证据。不据此宣称 ENet 用于给 Steam 加速、实时失败回退、反作弊或必需的公开 LAN 功能。

推荐：产品主线直接 Steam；课程里的 ENet 示例单独练习；后续只有实际本机多开调试或非 Steam 连接需求时再增加 ENet 适配。保留它是开发效率选择，不是 Steam 联机技术必需。

## 补充：发布前与本地测试

正式上线不是调用 Steamworks 测试的前提。官方 Testing On Steam 文档说明开发者访问、发布前测试授权与 Release Override Keys；自有 AppID 只要账号具有对应访问许可即可开发测试，不需公开销售。测试包也可从本地启动，Steam 客户端需运行并登录，正确指定 AppID；steam_appid.txt 用于开发启动上下文，不会授予应用许可，也不代表完成 Steam 构建上传。文件查找位置应遵循选用绑定及工作目录配置。

官方 SpaceWar 示例 AppID 为 480，适合初步 API/联机实验；它是共享示例身份，不能视为产品私有配置或正式发布身份。测试两端使用同一 AppID 和兼容消息协议；共享 Lobby 使用项目/协议标记筛选及会话准入检查，不能仅靠搜索过滤验证身份。

分三种测试：单实例测试 Steam 初始化和房间；同机多进程通过进程内/环回测试传输验证游戏协议；不同电脑、不同账号验证真实 Steam 好友邀请和网络连接。同一账号多开不等价于多个独立 Steam 玩家；对真实路径优先两台电脑两个账号。一台物理机可考虑隔离 VM 与第二账号，但需验证网络、图形和 SDK 环境，不承诺开箱即用。进程内传输应保留序列化和异步边界；本机 TCP 或 ENet 可验证跨进程路径，也不能替代真实 Steam 测试。

ENet 是方便本地多开的可选开发工具，不是上线前测试 Steam 的必需条件。推荐先 Steam 小型收发原型，日常战斗测试使用轻量 Local/LoopbackTransport；有跨进程调试需求再加入 TCP/ENet。

来源：https://partner.steamgames.com/doc/store/testing 、https://partner.steamgames.com/doc/sdk/api/example 、https://partner.steamgames.com/doc/sdk/api 。
