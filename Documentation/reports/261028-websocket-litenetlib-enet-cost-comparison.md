# 纯 .NET 服务端：WebSocket、LiteNetLib、ENet 成本对比

日期：2026-10-28。用户已明确选择纯 .NET 服务端。本文件为研究分析，尚未实施选型或制定标准。

## 结论与比较口径

针对当前 DBG 卡牌命令、房间内串行权威结算，推荐首先实现 ASP.NET Core/Kestrel + WSS。需要可靠 UDP 且愿意承担安全接入成本时优先比较 LiteNetLib；ENet 在已有原生技术积累或跨语言需求时合理。该结论是工程判断，没有进行三者性能基准，不能声称 WSS 更快或一定承载更多连接。

WebSocket 是协议，LiteNetLib、ENet 是库；实际比较对象为 .NET WebSocket 实现、LiteNetLib、ENet 的 C# 绑定及原生库。WSS 指 WebSocket 加 TLS。本文按常见 HTTP/1.1 WebSocket over TCP 比较，不将 SignalR 的 Hub、自动重连等能力算给原始 WebSocket，也不依赖 HTTP/2 多路复用。

## 共同成本

三者均需完成：Unity 依赖剥离、协议 DTO 与版本、序列化、身份绑定、房间路由、串行调度、请求去重、结果序号、快照恢复、超时、有界队列、慢客户端处理、监控与压测。这些很可能高于传输接入本身成本。

代码证据：Assets/Scripts/combat/BattleCommand.cs 与 BattleCommandManager.cs 已有权威队列分配 CommandId；该 Id 不能直接替代客户端重试请求 Id。Assets/Scripts/action/ActionExecutor.cs 仍依赖 MonoBehaviour/Coroutine；HandCardChooseAction 使用 WaitUntil；GameState 初始化访问 Unity Application 路径。选择任何网络库都不能消除这些纯 .NET 拆分工作。

## 开发成本

| 项目 | WebSocket/WSS | LiteNetLib | ENet + C# |
|---|---|---|---|
| 首个收发样例 | 低 | 低 | 低至中 |
| 完整传输适配 | 中：消息分片、发送串行、取消、关闭 | 低至中：消息、可靠模式、Poll 调度 | 中：额外原生资源生命周期 |
| Unity 集成 | 需选库并验证 Mono/IL2CPP 与主线程分发 | C# 游戏 API，需固定兼容版本 | 需匹配各平台原生库与绑定 |
| 认证加密 | TLS 标准链路，应用仍需认证 | 需选择并集成经过审查的安全通道/协议 | 同左，不能将普通连接当作加密认证 |
| 对局恢复 | 三者均需应用实现 | 同左 | 同左 |
| 调试工具 | 标准 WebSocket 工具加业务日志 | 库统计、应用测试客户端与日志 | 库统计、应用测试客户端与日志 |

原始 WebSocket 不自动提供房间、重连、序列化和游戏恢复。在 .NET 上应让同一连接的发送由单消费者队列串行执行，并按 EndOfMessage 拼合完整消息，限制累计大小，处理取消、关闭和超时。LiteNetLib/ENet 的完整包接口更贴近游戏消息，内置 RTT/丢包统计及可靠传输能力是切实优势。

LiteNetLib 推荐使用官方建议的源码或 Unity 包方式，固定经过验证的版本，不跟随 master。ENet-CSharp 是具体实现的例子，不代表所有 ENet 绑定：该项目声明修改协议，提供原生库且现成包覆盖传统 x64 平台，不能推断与原版互通或所有架构开箱可用。

WSS 有证书与 TLS 运维成本。可靠 UDP 的校验和、固定 connection key 都不能替代身份认证和机密性。安全实现不必自行发明，但需要选择、集成和验证现有方案，其成本应计入生产比较。

## 性能分析

1. 无丢包下的常规命令往返：三者都需经历实际网络路径；WSS 建立连接后不为每条消息重做 HTTP/TLS 握手。UDP 不自动节省一次命令往返。初次握手与重连费用另计。
2. 丢包：TCP 会阻塞同一连接中后续字节；可靠 UDP 的同一有序通道也需等待缺包。ENet/LiteNetLib 可隔离通道，但跨通道不保证全局顺序。若 B 必须等待 A 的战斗结果，多通道无法消除业务依赖。
3. 可丢弃数据：光标、瞄准、实时位置可以采用不可靠或最新状态语义，UDP 优势明显。出牌、选择、扣能量、结算事实不能这么处理；当前项目大多数核心消息属于后一类。
4. 发送调度：每 15ms 或 20ms Poll/Flush 才处理一次，会引入应用等待；WebSocket 的批处理也一样。示例代码的 Sleep 不应直接作为生产调度。应比较端到端延迟分布。
5. 带宽：WebSocket 支持二进制；不能拿 WSS+JSON 对比 UDP+二进制得出传输结论。RFC 6455 下，不超过 125 字节载荷的单帧基础头为服务端 2 字节、客户端 6 字节（含 mask）；总带宽还包含 TCP/IP、TLS、ACK、重传。UDP 同样包含 IP/UDP、库头、ACK、心跳与重传，库宣称的几字节头不等于线上总开销。
6. CPU/内存：WSS 消耗 TLS、TCP 缓冲和连接对象；可靠 UDP 消耗可靠队列、重传、分片与轮询状态；ENet 有原生内存与跨边界调用。必须在同等加密、同等可靠性及相同序列化条件下测试。单次跨边界调用不是自动瓶颈。
7. 房间串行结算只要求每房间顺序，不代表全服务器串行。慢客户端不能阻塞全房间或服务线程；三种方案均需限制发送积压。

仅作算术示例：一房间 4 人，每人平均每秒 2 条 100B 请求，服务端每条请求生成 1KB 结果并广播 4 人，则入口有效载荷约 800B/s，出口约 32KB/s。100 个活跃房间出口约 3.2MB/s，即 25.6Mbps，尚未计协议与重传。该例不是实际项目测量或房间容量预测；它说明广播、消息体积和并发可能比少量帧头更重要。快照突发、空闲心跳和事件展开需另计。

## 部署维护

单台阿里云轻量支持 TCP/UDP，三者都能运行。WSS 可以由 Kestrel 直接终止 TLS，也可经 Caddy/Nginx 后转发，反向代理不是必需组件。若使用代理需配置 WebSocket 支持与空闲超时、续证和长连接排空。端口 443 并不保证所有网络可达。

LiteNetLib 的单机 UDP 部署本身简单，需正确放行 UDP，容器映射也标明 UDP。普通 HTTP 反向代理不能直接代理 ENet/LiteNetLib；扩容时需 UDP/L4 转发及会话路由，或让客户端连接分配的战斗节点。ENet 另外要维护原生二进制、系统 ABI、CPU 架构、升级及崩溃排查。纯 .NET 主程序使用原生库在技术上可行；如果“纯 .NET”还要求完全托管，则通常的原生 ENet 绑定不符合该附加条件。

三者都是有状态长连接：WSS 通过负载均衡不等于房间状态自动迁移，更新都要处理停止接新房、对局排空或快照恢复。不能用普通 HTTP 请求限流代替升级后的游戏消息限流。所有方案都需独立观测连接数、队列、消息、恢复及业务结算耗时；WSS/TLS 本身不防所有攻击。

## 为什么推荐及反转条件

推荐依据：核心消息基本都可靠有序；现有 BattleCommand 与房间权威入口适合消息式服务；WSS 在认证加密和日常 .NET 服务运维方面可复用常规设施；目前缺少可证明值得增加可靠 UDP 接入成本的高频数据需求。

该推荐对成本的优势主要来自上线完整度，而非首个样例代码；若只比较局域网/未加密的游戏传输收发，LiteNetLib 甚至可能更省事。

当出现大量可丢弃高频消息、多种真正独立且需要隔离的消息流、真实弱网对照证实 WSS 延迟不达标，或团队已有成熟安全 UDP 接入时，应优先重新比较 LiteNetLib。已有 ENet 经验、跨语言库复用和原生部署流水线时，ENet 的额外成本会明显下降。

## 建议验证

先验证一个 Unity 客户端与纯 .NET WSS 服务的连接、二进制协议与断线恢复。主协议和业务层不暴露 WebSocket/Peer 类型，以小型传输适配器隔离接入，不提前实现三套。

如需对比压测，固定消息语义、加密、序列化、房间数和发送频率；分别测真实链路及可控延迟/抖动/丢包。记录 p50/p95/p99 命令结果延迟、CPU、托管与原生内存、实际出网、积压、恢复耗时；加入重复请求、突发大快照和慢客户端。容量判定还必须覆盖实际战斗模拟，而非仅 echo。

## 官方资料

- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket.sendasync?view=net-10.0
- https://www.rfc-editor.org/rfc/rfc6455
- https://github.com/RevenantX/LiteNetLib
- https://github.com/nxrighthere/ENet-CSharp
- https://github.com/lsalzman/enet/blob/master/docs/design.dox
- https://www.alibabacloud.com/help/zh/simple-application-server/user-guide/manage-the-firewall-of-a-server
