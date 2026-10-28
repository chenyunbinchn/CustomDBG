# CustomDBG 的 MVP、MVC、MVVM 对比与现状评估

日期：2026-09-08
范围：静态代码与设计文档核查；方案仅用于解释，未实施架构变更、未运行性能测试。

三种模式存在变体，没有统一的强制标准。本文使用被动视图式 MVP、经典 GUI MVC、通过绑定同步的 MVVM。
共同边界：业务状态与规则留在战斗领域层；所有出牌最终进入 BattleCommandApi；布局、拖拽和动画留在 View；UIManager 管理窗口层级和生命周期。

## MVP 的典型实现
BattleHUD/HandView/CardView 只渲染展示数据并发出 Intent。BattlePresenter 订阅 Intent，调用业务入口，并从状态生成 BattleHudDisplayData，主动调用 View.Render。可以用 IBattleHudView 和注入的业务入口隔离具体 Unity 组件，便于使用假 View 测试 Presenter；接口不是 MVP 定义的硬性条件。
出牌：View -> Intent -> Presenter -> BattleCommandApi -> 业务状态变化 -> Presenter -> Render。
牌堆弹窗由 Presenter 调用导航服务打开；复杂后可拆分牌堆 Presenter。
优点：流程显式，便于编排出牌反馈和动画，符合当前项目结构。代价：事件订阅、数据映射和刷新代码需要手写，Presenter 容易承担过多职责。

## MVC 的典型实现
经典 GUI MVC 中，Controller 处理输入并调用业务入口；View 观察 Model 的变化并通过只读接口取数据。可以有只读查询接口、显示格式化辅助类，Model 无需了解 View。
出牌：View 输入 -> Controller -> BattleCommandApi -> Model 变化通知 -> View 查询状态并重绘。
BattleController 负责出牌和导航；BattleHUD 订阅能量及手牌变化，CardView 只展示。
优点：Controller 可专注输入，多个 View 可观察同一状态。代价：View 更了解 Model 的查询结构，显示逻辑更容易散落在 Unity 组件中；多个变化通知需合并到一致的提交边界。
这一经典方案会改变项目目前“View 只接收展示快照”的规则；这里只作方案比较，不修改规则。不能仅将 BattlePresenter 改名 Controller 就认为完成 MVC 改造。

## MVVM 的典型实现
BattleHudViewModel 成为长期存在的展示状态对象，暴露能量文本、卡牌集合、选定目标、等待状态和出牌命令。它观察业务变化并更新可观察属性，绑定适配层同步到 Unity 控件；不持有具体 BattleHUD 或 TMP_Text。
出牌：View 手势 -> 命令绑定 -> ViewModel -> BattleCommandApi -> 业务变化 -> ViewModel 属性/集合变化 -> 绑定层 -> View。
UI 命令对象与领域 BattleCommand 是不同层次的对象；命令绑定不意味着 UI 可直接修改牌堆。只读战斗数据通常单向绑定，双向绑定不是 MVVM 必需条件。
拖拽坐标和 Tween 仍留在 View，语义上的目标选择、等待状态进入 ViewModel。导航可通过服务处理。当前项目需要新增绑定适配与解绑机制，不能只给现有数据类添加一个接口就算完成。
优点：大量属性同步更集中，展示状态和命令可脱离控件测试。代价：维护属性依赖、集合同步和订阅生命周期；多阶段战斗动画仍需额外编排。

## 当前实现的优势
- Intent 和 BattleCommand 分离，View 不直接改战斗状态。
- 只读展示数据隔离业务对象，生成与渲染链路明确。
- UIManager 不读业务状态，窗口管理与战斗交互职责分开。
- 手势、扇形布局、Tween 留在 View；Presenter 负责语义操作。
- 当前最小功能不需要额外绑定基础设施。

## 当前限制及代码依据
1. 刷新覆盖依赖显式调用。BattlePresenter.Tick 只在等待自己提交的命令结束或收到 Refresh 请求时刷新；UnityBoostrap 有外部 RefreshBattleHud 调用，因此不能说完全不处理外部变化。新增状态修改路径若未接入刷新，可能导致显示滞后。
2. 手动刷新与战斗稳定性不是同一个约束。Tick 对 _refreshRequested 的刷新只检查 UIManager.IsBusy，外部请求不一定等待 IsBattleStable。
3. HandView.Render 总是 Clear 后 Instantiate 全部卡牌。会重置交互/动画状态，并产生创建销毁开销；是否造成实际帧尖峰尚未测量。应先按 CardInstanceId 增量协调，再根据性能数据决定对象池。
4. 提交出牌后设置 _waitingForCommand，但没有同时请求刷新。Presenter 会拒绝后续出牌，界面却可能仍显示可操作，直到后续刷新；适合补充明确的等待视觉状态。
5. 用全局命令和动作队列空闲判断完成，没有对应某次命令的明确成功/失败结果。当前适合刷新最终状态，难表达中间抽牌、伤害和弃牌的动画顺序。应区分命令结果、状态提交通知和表现事件。
6. BattlePresenter 同时映射卡牌、检查可用性、处理命令、选默认目标、管理 HUD/牌堆事件和弹窗导航。后续复杂时可提取 Mapper、导航接口或独立展示协调逻辑。
7. Presenter 持有具体 BattleHUD/CardPileDialogModal、StateManager，并直接访问 UIManager.Instance 和静态 BattleCommandApi。虽然不是 MonoBehaviour，独立测试仍需较多环境；有真实测试需要时可引入最小接口。
8. UIManager 根操作串行并拒绝忙时请求；这简化生命周期，但弹窗/刷新体验受忙状态影响。它属于窗口管理策略，不是 MVP 限制。

建议：保持 MVP 风格，优先完善状态提交后的刷新入口、等待/失败反馈和按卡牌 Id 更新 View。再按功能与测试需求拆分 Presenter。仅为名称统一可以把当前 ViewModel 称作 DisplayData 或 ViewData，但没有必要为了模式名称进行批量改名。MVC/MVVM 的切换本身不会解决刷新覆盖、实例重建或动画编排问题。

代码依据：Assets/Scripts/ui/presenters/BattlePresenter.cs；Assets/Scripts/ui/controllers/BattleUIController.cs；Assets/Scripts/ui/core/UIView.cs；Assets/Scripts/ui/core/UIManager.cs；Assets/Scripts/ui/BattleHUD.cs；Assets/Scripts/ui/HandView.cs；Assets/Scripts/ui/CardView.cs；Assets/Scripts/ui/viewModels/BattleHudViewModel.cs；Assets/Scripts/mono/UnityBoostrap.cs。
项目约束：Documentation/rules/260825-rule-ui-framework.txt；Documentation/documentations/260826-ui-design.txt。
概念参考：
- https://www.martinfowler.com/eaaDev/uiArchs.html
- https://martinfowler.com/eaaDev/PassiveScreen.html
- https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm
