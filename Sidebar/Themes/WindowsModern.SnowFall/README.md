# Snowfall (Experimental) — Sidebar Theme

> **这是一份实验性/演示性主题。**  
> 它的目的不只是"换个皮肤"，而是**探讨主题系统的灵活度边界**：
> 一个主题能加载外部程序集、能在侧边栏上直接渲染自绘动画、能层叠自定义视觉元素、能覆盖任意基础样式。
> 如果你正在设计自己的主题，可以把它当作参考实现。

A quiet winter night with softly falling snow — and a working reference for how far a theme can go.

---

## Why "Experimental"?

Sidebar 的主题层原本只预期是**纯 XAML 资源字典**（配色、图片、少量 Storyboard）。SnowFall 演示了以下"超出常规"的能力：

| 能力 | 实现方式 | 文件位置 |
|---|---|---|
| **加载自定义程序集** | `xmlns:snow="clr-namespace:WindowsModern.SnowFall;assembly=WindowsModern.SnowFall"` 触发 `Assembly.Load`，走主项目的 `CurrentDomain_AssemblyResolve` | `Theme.xaml` 顶部 |
| **在主题里实例化自定义控件** | `<snow:SnowfallOverlay ... />` 直接作为 `SidebarBackground` 的子元素 | `Theme.xaml` → `SidebarBackground` |
| **自绘动画（非 WPF Storyboard）** | `SnowfallOverlay` 内部用 `DispatcherTimer(33ms)` + `OnRender` 画椭圆，不依赖 XAML 动画系统 | `SnowfallOverlay.cs` |
| **主题参数化** | 雪花数量/大小/风速/摆幅/颜色全部通过 `DependencyProperty` 暴露，主题 XAML 里像普通属性一样设置 | `SnowfallOverlay.cs` |
| **可复用的装饰器** | `SnowfallHost` 是一个 `Grid`，把任意内容塞进去就会自动叠加雪花层（给自定义主题作者用） | `SnowfallHost.cs` |
| **完全直角 Flyout** | `FlyoutBackground` 的 `CornerRadius="0"`，与磁贴直角边缘统一 | `Theme.xaml` → `FlyoutBackground` |

> 这个主题**不是**为了证明"主题越复杂越好"，而是为了给主题作者一个可以照抄的模板：**你能在主题里做到什么程度**。

---

## Files

| File | Purpose |
|---|---|
| `Theme.xaml` | 主题资源字典（配色 + 雪花叠加 + 自定义控件实例化） |
| `Locale.xml` | 本地化元数据（含描述的中英日韩法德西俄翻译） |
| `Logo.ico` | 可选：主题列表图标，需用户自备 |
| `README.md` | 本文件 |

程序集 `WindowsModern.SnowFall` 的源码单独放在 `WindowsModern.SnowFall.csproj` 项目里。

---

## Deployment

1. 将 `SnowFall` 整个文件夹复制到 `Themes/SnowFall/`。
2. **重要**：把 `WindowsModern.SnowFall.dll` 复制到 **`Sidebar.exe` 同目录**
   （不是只放在主题文件夹里）。
   原因：`App.xaml.cs` 的 `CurrentDomain_AssemblyResolve` 在首次解析时只会去
   `AppDomain.BaseDirectory` 找 `xxx.dll`，`Themes/SnowFall/` 里的 DLL
   需要额外触发索引重建才能被加载，因此**放到 exe 同目录是唯一开箱即用的部署方式**。
3. （可选）在 `Themes/SnowFall/` 放一个 32×32 或 48×48 的 `Logo.ico` 作为主题列表图标。

---

## Customizing the snow

打开 `Theme.xaml`，找到 `SidebarBackground` 里的：

```xml
<snow:SnowfallOverlay
    SnowflakeCount="75"
    SnowflakeSize="2.4"
    SpeedFactor="1.0"
    Wind="0.15"
    SwayFactor="1.2"
    BaseOpacity="0.95"
    SnowColor="#FFFFFF" />
属性	默认值	含义
SnowflakeCount	75	雪花数量（建议 30–150）
SnowflakeSize	2.4	基础大小（px），每片雪花实际大小在 0.5× 到 1.6× 之间随机
SpeedFactor	1.0	下落速度倍率
Wind	0.15	横向风力（0 = 垂直下落，正值向右飘）
SwayFactor	1.2	左右摆动幅度
BaseOpacity	0.95	雪花最大不透明度（最终透明度 = 此值 × 随机 0.55–1.0）
SnowColor	#FFFFFF	雪花颜色（ARGB）
Palette reference
元素	颜色	说明
主渐变 0%	#FF3A5A82	左侧雪蓝
主渐变 45%	#FF2A4268	中间过渡
主渐变 100%	#FF16243C	右侧深蓝
顶部月晕内圈	#66C8E8FF	半透明亮冰蓝
底部微光	#40A0C8E8	25% 透明冷蓝
左侧亮条最外	#FFA8D8F8	不透明亮冰蓝（对应 Halloween 的亮橙）
顶部高光线	#80D8ECFF	半透明白蓝
雪花	#FFFFFF	纯白
想让背景更亮：把主渐变左右端各提一档（比如 #FF4A6A94 / #FF22344E），月晕内圈提到 #80D8F0FF。
想回到更暗的原版雪夜：主渐变改回 #FF1C2E4A / #FF0E1A2E，月晕回 #3AB8D8F0，雪花回 #F0F6FF。

Using SnowfallHost in a custom theme
如果你在写自己的主题，想让 Flyout / Notification 也飘雪，把 SnowfallHost 包在外面：

xml
<snow:SnowfallHost SnowflakeCount="30" SnowflakeSize="1.8">
    <Grid>
        <!-- 原有 Flyout 内容 -->
    </Grid>
</snow:SnowfallHost>
SnowfallHost 是一个 Grid，雪花层固定为第一个子元素且 ZIndex=1000，你后续加的任意内容都会自动排在雪花下方。

注意事项
1. VSM 动画必须用 Brush 对象，不能写字符串
TileIconItemButtonStyle / TileIconItemWithTextButtonStyle 里的
DiscreteObjectKeyFrame 必须使用 {StaticResource ...} 引用共享
SolidColorBrush（SnowTileHoverBackground 等），不能直接写颜色字符串。
WPF 的 ObjectAnimationUsingKeyFrames 不经过 BrushConverter，
字符串会导致 AnimationException: 值不是此属性的有效值。

xml
<!-- ❌ 错误 -->
<DiscreteObjectKeyFrame KeyTime="0" Value="#3370A8D8" />

<!-- ✅ 正确 -->
<DiscreteObjectKeyFrame KeyTime="0" Value="{StaticResource SnowTileHoverBackground}" />
ColorAnimation 不受此限制（它操作 Color 子属性，走 ColorConverter），
所以 TileSplliterTemplate / SidebarOptionsSplliterTemplate 里的 ColorAnimation 不用改。

2. 雪花层不拦截鼠标
snow:SnowfallOverlay 的 IsHitTestVisible="False"，
不会挡住下方磁贴的点击、拖拽、右键菜单。

3. 左右镜像
侧边栏切到左侧（SidebarDirection.Left）时，
RootGrid 的 ScaleTransform ScaleX="-1" 会把雪花层一起镜像，
风力方向也随之反转，视觉上仍然一致。

4. 性能
默认 75 片雪花 × 30 FPS × 1 次 DrawEllipse ≈ 2250 次绘制/秒，
CPU 占用 < 1%，内存 < 100 KB。
如果觉得太热闹，把 SnowflakeCount 降到 30–40。

Build the assembly
bash
msbuild WindowsModern.SnowFall.csproj /p:Configuration=Release
输出到 Themes/SnowFall/WindowsModern.SnowFall.dll。记得再复制一份到 Sidebar.exe 同目录。

License
Same as Sidebar. Copyright © 2026 Windows Modern. All rights reserved.

text

---

## 三、部署清单
Sidebar.exe
├── WindowsModern.SnowFall.dll ← 必须放这里（从 Themes/SnowFall/ 复制一份）
├── WindowsModern.SnowFall.pdb ← 可选，调试用
└── Themes/
└── SnowFall/
├── Theme.xaml
├── Locale.xml
├── README.md
├── WindowsModern.SnowFall.dll ← 构建产物，也留一份在这里
└── Logo.ico ← 可选

text

**关键提醒**：构建完 `WindowsModern.SnowFall` 项目后，除了 `Themes/SnowFall/` 里会生成一份 DLL，**务必再手动复制一份到 `Sidebar.exe` 同目录**。这是 `App.xaml.cs` 的 `CurrentDomain_AssemblyResolve` 的 fallback 路径要求，否则切到 SnowFall 主题时 `xmlns:snow="...assembly=WindowsModern.SnowFall"` 会解析失败，主题加载时报错。

如果想让构建自动复制，可以在 `WindowsModern.SnowFall.csproj` 里加 `PostBuildEvent`：

```xml
<Target Name="AfterBuild">
  <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(SidebarExeDir)" SkipUnchangedFiles="true" Condition="'$(SidebarExeDir)' != ''" />
</Target>
或者直接在 Sidebar 主项目里把这个 DLL 作为 Content 添加、CopyToOutputDirectory=PreserveNewest。