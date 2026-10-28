# 当前 UI 框架与团队 UI 生产流程对比

日期：2026-09-20
范围：静态阅读项目 UI 代码与已有设计；公司流程仅依据用户描述，不推断其内部实现。未修改运行代码，未运行 Unity。

## 核心结论
当前项目主要解决运行期职责分离，公司描述主要涉及资源生产、配置交付、协议协作，两者可以组合。PSD 转 JSON 转 Prefab 不能据此判断其运行期采用 MVP、MVC 或 MVVM，也不意味着支持运行时 JSON 布局或热更新。

## 项目依据
- Assets/Scripts/ui/core/UIManager.cs：管理 Page、HUD、Modal、Overlay；根操作共用 busy 标记，忙时拒绝请求；Refresh 也受限。
- Assets/Scripts/ui/core/UIView.cs：Bind 主动调用 Render，没有属性通知或自动绑定。
- Assets/Scripts/ui/presenters/BattlePresenter.cs：业务状态转只读展示模型，Intent 转 BattleCommand，等待业务队列稳定后刷新。
- Assets/Scripts/ui/BattleHUD.cs：序列化引用绑定控件，渲染并转发事件。
- Assets/Scripts/ui/HandView.cs：每次 Render 清理并重建 CardView；增量更新、池化和表现事件仍为 TODO。
- Assets/Scripts/ui/loading/AddressablesUIViewLoader.cs：资源加载、实例销毁与句柄释放；尚无共享预加载缓存。

优势：边界清楚，业务不落入控件，流程易追踪，框架投入小。
局限：人工绑定和运行时检查较多；没有在本次检查中发现 PSD 布局导入和绑定生成链路；复杂并发、异步返回、表现连续性需要扩展。

## 团队流程的收益与代价
配表将规则和内容交给策划，但需类型、范围、引用和版本校验。设计稿导入减少重复布局，但必须处理组件语义、字体、九宫格、锚点、动态文本和屏幕适配。协议协作支持并行开发，但需定义状态权威、错误、时序与兼容。以上收益均依赖实际规则与工具，不因公司规模或使用 JSON 自动成立。

尤其需要检查二次导入：是否保留节点身份、程序引用、事件和人工调整；生成区和手工区如何划分；重复导入是否产生无意义 diff。Prefab Variant 或嵌套 Prefab 可辅助组织，但不自动保证导入安全。

## 建议顺序
1. 保留 View -> Intent -> Presenter -> 业务，以及业务 -> 展示模型 -> Render 的边界。
2. 定义最小交付约定：控件稳定标识、类型、动态区域、资源和文案标识、生成与手工修改归属。
3. 优先增加编辑器校验与绑定辅助；若生成代码，只生成引用和绑定，不生成业务逻辑。
4. 使用 Mock 展示数据覆盖空、满、长文本、加载、失败、禁用等状态，解耦前后端进度。
5. 区分静态配置、服务器状态和展示状态；协议数据经业务状态映射后交给 View。
6. 页面数量和美术返工成本确有需要时，再投入完整 PSD 导入器；运行期优化按实际场景和性能测量推进。

新人可用一个完整页面跟踪：策划修改、资源交付、首次导入、程序绑定、协议联调、美术二次导入、适配验收。优先理解公司现有约定，再提小范围改进。

## 官方参考
Unity Prefab Variants：https://docs.unity.com/en-us/engine/6000.3/manual/working-with-gameobjects/prefabs/prefab-variants
Unity Auto Layout：https://docs.unity3d.com/ja/2022.3/Manual/UIAutoLayout.html
Unity 图像与字体准备：https://docs.unity.com/en-us/engine/6000.0/manual/best-practice-guides/bpg-uiad-index/graphic-and-font-assets-preparation
