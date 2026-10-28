# ENet 学习试点与课程核查

日期：2026-10-28。新增前提：用户以学习为主，课程尚未完成，希望为未来实时多人游戏积累经验，当前选择不以最低产品上线成本为唯一目标。

结论：在该前提下，推荐可以调整为使用 ENet。CustomDBG 是合适的第一阶段试点，可用于学习连接、消息、序列化、权威校验、房间、排序、超时、恢复及云部署；不能据此验证高频动作同步表现。此前推荐 WSS 针对完整上线成本，未充分计入教程可用性和知识迁移收益。

## 本地课程证据

课程路径：E:\BaiduNetdiskDownload\联机游戏开发完全指南-课程资料。

- 程序工具/enet-1.3.18.tar.gz 存在，tar 目录核实为 enet-1.3.18。
- 课件示例/README.txt 要求 Visual Studio Release/x64 编译，示例项目为 C++。
- 课件示例/Demo_1_Client/client.cpp 第 18 行用 Winsock socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)，是直接 TCP 示例。
- 课件示例/Demo_5_Server/server.cpp 第 19 行用 enet_host_create；第 43 行构建 ENET_PACKET_FLAG_RELIABLE 包，第 44 行 enet_peer_send。这是可靠 UDP 示例，RELIABLE 不会将传输切换到 TCP。
- Demo_7_Server/server.cpp 也使用 enet_host_create 和 ENet 可靠消息。

仅核查目录及上述代表性源码，没有观看视频或完整审阅课程，不判断完整课程覆盖范围。

## ENet 与 TCP 的准确区分

ENet Host/Peer 提供 UDP 上的连接管理、可靠性和通道，不提供将同一套 Host/Peer 自动切换到 TCP 的传输模式。原版公开头文件确实有 ENET_SOCKET_TYPE_STREAM，底层跨平台 socket 包装具备流式 socket 能力，因此“源码里有 TCP 相关能力”有依据；但这不等于 ENet 的可靠消息协议支持 TCP。官方 host.c 的 enet_host_create 使用 ENET_SOCKET_TYPE_DATAGRAM。使用低层流 socket 则需自行实现 TCP 通信及应用协议。

课程同时存在直接 TCP 与 ENet UDP 示例，可能造成把两部分能力统称为 ENet 的理解；不能断言视频具体说了什么。

## 为什么试点合理及其边界

卡牌战斗降低了物理、插值、预测等变量，方便观察消息与权威状态的因果关系。无需为“充分利用 UDP”而把出牌与结算改为不可靠消息。第一阶段将关键战斗消息放在可靠有序通道，保留服务端排序和客户端请求去重。

未来的世界、怪物、3D 模型和动画本身不决定 UDP 需求。实时移动、物理互动、战斗响应、同步频率、可丢弃消息和带宽才决定传输要求。通常同步实体 Id、位置/朝向、状态、动作触发与服务端时间，模型/贴图等资源通过独立资源分发，动画在本地驱动；是否同步动画状态取决于玩法权威需求。

ENet 的 UDP 和可靠性不能保证低延迟或永不掉线。可靠包重传会等待；把所有位置消息也可靠发送可能增加积压。流畅动作游戏还需要快照、插值、预测、协调、相关性筛选与带宽管理。CustomDBG 可以验证网络基础，不能代替这些实时场景实验。

建议阶段：先跟课运行原始小例子；在独立 .NET 小程序验证 C# 绑定连接与收发；接入 CustomDBG 权威命令、超时和恢复；再做独立的小型移动同步实验验证可靠/不可靠、丢包、插值与预测。无需把 CustomDBG 改造成动作游戏。

课程原版 ENet 1.3.18 与选用 C# 实现要核实兼容性。例如 ENet-CSharp 自称修改协议，不能默认与课程 C++ 原版端互通。正式项目两端统一选定实现与版本；迁移概念而不是机械复制函数。

## 官方核查来源

- https://github.com/lsalzman/enet/blob/master/docs/design.dox
- https://github.com/lsalzman/enet/blob/master/include/enet/enet.h
- https://github.com/lsalzman/enet/blob/master/host.c
- https://github.com/lsalzman/enet/blob/master/unix.c
- https://github.com/nxrighthere/ENet-CSharp

仅记录研究与建议，未修改代码、课程资料或项目架构标准。
