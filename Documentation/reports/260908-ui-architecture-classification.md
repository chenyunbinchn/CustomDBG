# UI 架构核查

日期：2026-09-08

结论：当前战斗 UI 属于 MVP 风格，使用只读 ViewModel 作为展示数据快照；不是典型的数据绑定式 MVVM。

依据：
- Assets/Scripts/ui/presenters/BattlePresenter.cs：读取战斗状态、创建 ViewModel、订阅 View 的 Intent 事件、提交 BattleCommand，并通过 UIManager.Refresh 主动刷新 View。
- Assets/Scripts/ui/BattleHUD.cs：Render 将展示数据赋值给控件，用户操作通过事件上报。
- Assets/Scripts/ui/viewModels/BattleHudViewModel.cs：只有构造器与只读展示属性，没有属性变更通知或命令绑定。
- Assets/Scripts/ui/core/UIView.cs：Bind 直接调用 Render，没有持续数据绑定。
- Assets/Scripts/ui/controllers/BattleUIController.cs：负责场景接入、Presenter 创建、Tick 和销毁；Controller 命名不改变主要交互链路的 MVP 性质。
- Documentation/documentations/260826-ui-design.txt：明确描述 Presenter 转换 Intent、重建 ViewModel 并刷新 HUD。

交互链路：View -> Intent -> Presenter -> BattleCommand -> 业务状态；Presenter -> 展示数据快照 -> View.Render。

范围：静态核查当前代码与已有设计文档，未修改运行代码，未执行运行测试。UIManager 本身负责窗口层级与生命周期；MVP 分类主要针对其上的战斗 UI 交互架构。
