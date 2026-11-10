（由AI生成该文档）

[English](README.en.md)

# Snowfall（落雪）— 官方实验主题与主题能力范本

> **状态：随 Sidebar 发布的官方主题之一，同时是一个实验性、演示性主题。**
>
> Snowfall 不只是“换一套配色”。它是一份可以运行的主题能力范本：主题如何加载外部程序集、如何在纯 XAML 资源字典里实例化自定义控件、如何把自绘动画叠进 Sidebar 的视觉树、如何把参数暴露给主题作者，以及如何跟随 Windows 的浅色/深色设置。阅读它，比阅读一份只讲资源键的文档更直观。

> 视觉方向：深色模式是雪夜蓝渐变 + 白色飘雪，浅色模式是偏亮的冰蓝渐变；实际效果由 `Theme.xaml` 和 `SnowfallOverlay` 实时绘制。

---

## 1. 它是什么，不是什么

**是：**

- 一个完整的 Sidebar 主题：`Theme.xaml` 定义配色、背景、Flyout、拖拽窗、通知、磁贴交互样式。
- 一个独立程序集 `WindowsModern.SnowFall.dll`：里面是自绘雪花控件、装饰器和配色提供者。
- 一个“主题 + 代码”协同工作的参考实现：主题 XAML 直接引用程序集里的类型，程序集又反过来读取系统主题状态。
- 一个可以直接复制、改名、改色后作为自己主题起点的模板。

**不是：**

- 不是 Sidebar 核心的一部分；删掉整个主题文件夹不会影响 Sidebar 启动。
- 不是通用动画库；雪花只服务于这个主题的视觉表达。
- 不是“主题必须这么写”的规范；它刻意演示了边界能力，普通主题仍然可以只写 XAML。
- 不是像素级固定的皮肤；雪花数量、颜色、速度和深浅色配色都可以改。

---

## 2. 它演示了什么

| 能力 | 实现方式 | 关键文件 |
|---|---|---|
| 主题加载外部程序集 | `xmlns:snow="clr-namespace:WindowsModern.SnowFall;assembly=WindowsModern.SnowFall"` 触发程序集解析 | `Theme.xaml` 顶部 |
| 在主题 XAML 中实例化自定义控件 | `<snow:SnowfallOverlay ... />` 直接作为 `SidebarBackground` 和 `FlyoutBackground` 模板的子元素 | `Theme.xaml` |
| 自绘动画，不依赖 WPF Storyboard | `SnowfallOverlay` 用 `DispatcherTimer(33 ms)` 驱动 `OnRender`，逐帧画椭圆 | `SnowFall/SnowfallOverlay.cs` |
| 通过依赖属性参数化 | 雪花数量、大小、速度、风力、摆幅、透明度、颜色都是 DP，可在 XAML 里像普通属性一样设置 | `SnowFall/SnowfallOverlay.cs` |
| 可复用的视觉树装饰器 | `SnowfallHost` 是一个 `Grid`，把任意内容放进去，雪花层自动叠在最上面 | `SnowFall/SnowfallHost.cs` |
| 主题配色跟随系统 | `SnowfallThemeProvider` 读取 Windows 个性化注册表，监听系统偏好变化，通过 `INotifyPropertyChanged` 刷新绑定 | `SnowFall/SnowfallThemeProvider.cs` |
| 左右方向自适应 | 侧边栏反向时，模板根网格用 `ScaleTransform ScaleX="-1"` 镜像，雪花和风力一起翻转 | `Theme.xaml` 的 `DataTrigger` |
| 与插件磁贴共存 | 主题覆盖全局画刷和交互样式；插件磁贴继续使用自己的布局，通过 `DynamicResource` 取主题颜色 | `Theme.xaml` + 各 tile 的资源键 |

---

## 3. 运行方式

### 3.1 主题如何被加载

Sidebar 在启动时扫描程序目录下的 `Themes` 和 `Gadgets`：

```text
Sidebar.exe
├── Themes/
│   └── WindowsModern.SnowFall/
│       ├── Theme.xaml
│       ├── Locale.xml
│       ├── Mkho-Christmas-Snow-flake.ico
│       ├── README.md
│       ├── WindowsModern.SnowFall.dll
│       └── WindowsModern.SnowFall.pdb
└── Gadgets/
```

`Themes` 下的 `Theme.xaml` 是 `ResourceDictionary`。当 XAML 解析到：

```xml
xmlns:snow="clr-namespace:WindowsModern.SnowFall;assembly=WindowsModern.SnowFall"
```

WPF 会请求加载 `WindowsModern.SnowFall`。`App.xaml.cs` 的 `CurrentDomain_AssemblyResolve` 会：

1. 在 `Themes`、`Gadgets` 的递归索引里查找同名 DLL；
2. 找不到时再查 `Sidebar.exe` 同目录的 `<assembly>.dll`；
3. 建立索引有约 10 分钟缓存，运行中安装主题时建议重启 Sidebar。

因此，**DLL 放在主题文件夹里即可**，不需要额外复制到 `Sidebar.exe` 同目录。旧版说明里“必须复制到 exe 同目录”的说法已经过时；只有当你把 DLL 放在 `Themes`/`Gadgets` 之外时，才需要 fallback 路径。

### 3.2 仓库中的文件布局

```text
Sidebar/
├── SnowFall/                              # 独立程序集源码
│   ├── SnowFall.csproj
│   ├── SnowfallOverlay.cs
│   ├── SnowfallHost.cs
│   ├── SnowfallThemeProvider.cs
│   ├── Snowflake.cs
│   └── Properties/AssemblyInfo.cs
└── Sidebar/Themes/WindowsModern.SnowFall/ # 主题包
    ├── Theme.xaml
    ├── Locale.xml
    ├── README.md
    └── Mkho-Christmas-Snow-flake.ico
```

构建后，DLL 和 PDB 会出现在 `Debug/Themes/WindowsModern.SnowFall/` 或 `Release/Themes/WindowsModern.SnowFall/`；主题文件夹里的 XAML、Locale、图标由主项目作为 Content 复制到输出目录。

### 3.3 构建

需要 Visual Studio 2015（C# 6.0）和 .NET Framework 4.0。`SnowFall.csproj` 已加入 `Sidebar.sln`。

```bat
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" ^
  SnowFall\SnowFall.csproj /t:Build /p:Configuration=Debug
```

Release：

```bat
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" ^
  SnowFall\SnowFall.csproj /t:Build /p:Configuration=Release
```

输出路径由项目文件固定为：

```text
..\Debug\Themes\WindowsModern.SnowFall\
..\Release\Themes\WindowsModern.SnowFall\
```

如果只构建 `Sidebar.csproj`，主项目会复制主题包内容，但**不会**替你构建 `SnowFall` 程序集。首次构建或清理输出后，请先构建 `SnowFall`，或者直接构建整个解决方案。

### 3.4 安装与切换

1. 把 `Sidebar/Themes/WindowsModern.SnowFall/` 整个文件夹复制到运行目录的 `Themes/WindowsModern.SnowFall/`。
2. 确认 `WindowsModern.SnowFall.dll` 在该文件夹内。
3. 启动 Sidebar，在主题列表中选择 **Snowfall / 落雪**。
4. 如果 Sidebar 已经在运行，重新启动一次，避免程序集索引缓存尚未刷新。

---

## 4. 视觉结构

### 4.1 Sidebar 背景

`Theme.xaml` 的 `SidebarBackground` 模板大致是两层：渐变底层 + 雪花覆盖层。

```xml
<Grid x:Name="RootGrid" Background="Transparent"
      RenderTransformOrigin="0.5,0.5" ClipToBounds="True">
    <Grid.RenderTransform>
        <ScaleTransform ScaleX="1" />
    </Grid.RenderTransform>

    <!-- 1. 雪夜蓝渐变 -->
    <Border>
        <Border.Background>
            <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                <GradientStop Offset="0"
                    Color="{Binding SidebarBgStartColor,
                        Source={x:Static snow:SnowfallThemeProvider.Instance}}" />
                <GradientStop Offset="1"
                    Color="{Binding SidebarBgEndColor,
                        Source={x:Static snow:SnowfallThemeProvider.Instance}}" />
            </LinearGradientBrush>
        </Border.Background>
    </Border>

    <!-- 2. 自绘雪花层 -->
    <snow:SnowfallOverlay
        SnowflakeCount="75"
        SnowflakeSize="2.4"
        SpeedFactor="1.0"
        Wind="0.15"
        SwayFactor="1.2"
        BaseOpacity="0.9"
        SnowColor="#FFFFFFFF" />
</Grid>
```

- 渐变方向是左上到右下，深色模式下是深雪夜蓝，浅色模式下是偏亮的冰蓝。
- 雪花层最后添加，因此位于渐变之上；它不拦截鼠标，不会影响磁贴点击。
- 模板根网格开了 `ClipToBounds`，雪花不会画到 Sidebar 边界之外。

### 4.2 Flyout 背景

`FlyoutBackground` 使用独立的 `SnowfallOverlay`，参数更克制：

```xml
<snow:SnowfallOverlay
    SnowflakeCount="30"
    SnowflakeSize="2.0"
    SpeedFactor="0.9"
    Wind="0.12"
    SwayFactor="1.0"
    BaseOpacity="0.9"
    SnowColor="#FFFFFFFF" />
```

Flyout 的边框 `CornerRadius="0"`，与磁贴的直角边缘保持一致；雪层仍然不参与命中测试。

### 4.3 左右镜像

Sidebar 支持左右两侧。主题在多个模板根网格上使用同一个触发器：

```xml
<DataTrigger Binding="{StaticResource UtilSidebarIsReversedBinding}" Value="True">
    <Setter TargetName="RootGrid" Property="RenderTransform">
        <Setter.Value>
            <ScaleTransform ScaleX="-1" />
        </Setter.Value>
    </Setter>
</DataTrigger>
```

镜像后，雪花的下落方向、横向风力和亮边位置都会跟着翻转，不需要在代码里判断侧边栏方向。

---

## 5. 可调参数

`SnowfallOverlay` 和 `SnowfallHost` 暴露同一组依赖属性。

| 属性 | 类型 | 默认值 | 作用 |
|---|---|---:|---|
| `SnowflakeCount` | `int` | `60` | 雪花数量。主题 Sidebar 用 75，Flyout 用 30。 |
| `SnowflakeSize` | `double` | `2.0` | 基础直径（像素）；每片实际大小在 `0.5×` 到 `1.6×` 之间随机。 |
| `SpeedFactor` | `double` | `1.0` | 下落速度倍率，内部有下限保护。 |
| `Wind` | `double` | `0.1` | 横向风力；`0` 为垂直下落，正值向右。 |
| `SwayFactor` | `double` | `1.0` | 左右摆动幅度倍率。 |
| `BaseOpacity` | `double` | `0.85` | 最大不透明度；每片实际值再乘 `0.55–1.0` 的随机系数。 |
| `SnowColor` | `Color` | `#F0F6FF` | 雪花颜色（ARGB）。主题里用纯白。 |
| `SnowflakeStrokeColor` | `Color?` | `null` | 可选描边色；设置后先画略大的描边圆，再画雪心，适合浅色背景。 |

几个实用组合：

```xml
<!-- 安静、稀疏 -->
<snow:SnowfallOverlay SnowflakeCount="30" SnowflakeSize="1.8"
                      SpeedFactor="0.8" BaseOpacity="0.75" />

<!-- 暴风雪 -->
<snow:SnowfallOverlay SnowflakeCount="140" SnowflakeSize="3.0"
                      SpeedFactor="1.6" Wind="0.45" SwayFactor="1.6" />

<!-- 浅色背景：加描边 -->
<snow:SnowfallOverlay SnowColor="#FFFFFFFF"
                      SnowflakeStrokeColor="#802A4268" />
```

---

## 6. 用 SnowfallHost 把雪叠加到任意内容

`SnowfallHost` 是一个 `Grid` 装饰器。它在构造函数里先加入雪花层，并把 `ZIndex` 设为 `1000`；之后加入的内容自然排在雪花下方。

```xml
<snow:SnowfallHost SnowflakeCount="30" SnowflakeSize="1.8"
                   SpeedFactor="0.9" Wind="0.1">
    <Grid>
        <!-- 你自己的 Flyout / Notification / 自定义面板 -->
    </Grid>
</snow:SnowfallHost>
```

注意：

- `SnowfallHost` 是“覆盖层”思路，不会改变内容布局；内容仍然按自己的尺寸测量。
- 雪花层 `IsHitTestVisible="False"`，按钮、滚动条、右键菜单都还能正常操作。
- 如果内容本身不透明，雪花会被内容盖住；要让雪可见，内容背景需要留出透明区域，或者把 `SnowfallHost` 放在内容外层、让内容背景半透明。
- 一个窗口里不要嵌套多个 `SnowfallHost`，否则会叠加多套计时器，视觉和性能都不划算。

---

## 7. 配色与深色模式

`SnowfallThemeProvider` 是一个单例，暴露 `Instance`。它做三件事：

1. 读取注册表：
   ```text
   HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
       SystemUsesLightTheme
       AppsUseLightTheme
   ```
   值为 `0` 视为深色，`1` 视为浅色；读不到时按浅色处理。
2. 订阅 `SystemEvents.UserPreferenceChanged`；回调切回 UI 线程后重新读取。
3. 通过 `INotifyPropertyChanged` 通知所有 `{Binding ... Source={x:Static snow:SnowfallThemeProvider.Instance}}` 刷新。

暴露的状态：

- `IsShellDark`：Shell（任务栏/开始菜单）是否为深色。
- `IsAppDark`：应用主题是否为深色。

配色属性分几组：

- 文字：`ForegroundColor`、`ForegroundSoftColor`
- 边框/边缘：`BorderColor`、`EdgeColor`
- 分隔条：`SplitTopColor`、`SplitBottomColor`、`SplitTopHoverColor`、`SplitBottomHoverColor`
- 箭头按钮：`ArrowColor`、`ArrowHoverColor`、`ArrowPressedColor`
- 交互：`HoverColor`、`HoverStrongColor`
- 背景：`SidebarBgStartColor`、`SidebarBgEndColor`、`TaskbarBgStartColor`、`TaskbarBgEndColor`、`FlyoutBgStartColor`、`FlyoutBgEndColor`、`DragBgColor`、`NotifBgColor`、`NotifTextBgColor`
- 时钟投影：`ClockShadowColor`

当前实现中，**配色以 `IsShellDark` 为准**；`IsAppDark` 只作为状态暴露，不参与颜色选择。这样 Sidebar 的雪夜背景和任务栏/开始菜单的深浅保持一致，不会出现“Shell 深色、应用浅色”时背景跳变。

`Theme.xaml` 里只绑定 `Color`，不直接绑定 `Brush`：

```xml
<SolidColorBrush x:Key="GlobalForegroundBrush"
    Color="{Binding ForegroundColor,
        Source={x:Static snow:SnowfallThemeProvider.Instance}}" />
```

这样做是为了避免跨线程创建/冻结 `Brush` 的问题；颜色变化时由 WPF 重新生成画刷。

---

## 8. 自定义这个主题

### 8.1 改配色

优先改 `SnowfallThemeProvider.cs` 里的两组颜色（深色/浅色分支），而不是在 `Theme.xaml` 里硬编码。这样切换系统主题时仍然一致。

如果只想改这个主题、不打算复用程序集，也可以把 `Theme.xaml` 里的绑定换成固定颜色；但要注意 Flyout、拖拽窗、通知窗的配色是共享的，改一处会影响多个窗口。

### 8.2 改雪量

改 `Theme.xaml` 里两个 `SnowfallOverlay` 的 `SnowflakeCount` / `SnowflakeSize`。建议范围：

- Sidebar：30–150 片；超过 150 片在低端机器上会明显增加绘制量。
- Flyout：15–50 片；Flyout 面积小，太多会显得拥挤。
- 如果只想要静态氛围，把 `SnowflakeCount` 设为 `0`，计时器仍会启动但不会绘制雪花；更好的做法是直接移除 `SnowfallOverlay` 元素。

### 8.3 改动画行为

`SnowfallOverlay` 的帧间隔是固定的 33 ms（约 30 FPS）。要改速度，优先调 `SpeedFactor`，不要改计时器间隔：间隔太小会增加 UI 线程压力，太大则动画不连贯。

每片雪花的初始大小、速度、摆动相位、透明度都在 `ResetFlake` 里随机生成；`RebuildFlakes` 在尺寸变化或结构属性变化时重建雪花池。

### 8.4 视觉状态动画的坑

主题里的 `TileIconItemButtonStyle` / `TileIconItemWithTextButtonStyle` 使用 `ObjectAnimationUsingKeyFrames`。WPF 的 `DiscreteObjectKeyFrame` 不经过 `BrushConverter`，所以：

```xml
<!-- 错误：字符串不是 Brush 对象 -->
<DiscreteObjectKeyFrame KeyTime="0" Value="#3370A8D8" />

<!-- 正确：引用共享 Brush -->
<DiscreteObjectKeyFrame KeyTime="0"
    Value="{StaticResource SnowTileHoverBackground}" />
```

`ColorAnimation` 不受这个限制，因为它操作的是 `Color` 子属性，会走 `ColorConverter`；`TileSplliterTemplate`、`SidebarOptionsSplliterTemplate` 里的颜色动画可以继续用颜色字符串。

### 8.5 性能边界

- 雪花是逐帧 `OnRender` 绘制的椭圆，不是 WPF 动画对象；没有 Storyboard、没有每片雪花的 `FrameworkElement`。
- 计时器只在控件 `Loaded`、可见、且有实际尺寸时运行；`Unloaded` 或不可见时会停止。
- 绘制使用按透明度分档缓存的 `SolidColorBrush`，避免每帧新建画刷。
- 真正影响开销的是 `SnowflakeCount × 帧率`；如果出现卡顿，先降数量，再降 `SnowflakeSize`。

---

## 9. 设计边界与已知限制

- **不是 3D/物理模拟**：雪花只做匀速下落 + 正弦摆动 + 风力偏移，没有碰撞、堆积、融化。
- **不改变布局**：`SnowfallOverlay` 是覆盖层，不参与测量；它不会把内容“顶开”。
- **不拦截输入**：这既是优点也是限制；如果你想让雪花可点击，需要自己改 `IsHitTestVisible` 并实现命中测试。
- **多显示器/多窗口**：每个 Sidebar/Flyout 实例各自持有一个计时器；窗口越多，总绘制量越大。
- **主题切换**：`SnowfallThemeProvider` 是单例，生命周期跟随进程；它没有在主题卸载时自动 `Dispose`。当前实现假设 Sidebar 进程内只使用一个 Snowfall 主题实例。
- **DPI**：雪花大小使用 WPF 设备无关单位，缩放时跟随系统 DPI；没有单独的 DPI 感知逻辑。
- **辅助功能**：纯装饰动画不提供“减少动态效果”开关；如果系统开启了“减少动画”，当前实现不会自动降级。

---

## 10. 许可与素材

- `SnowFall/` 下的 C# 代码、`Theme.xaml`、`Locale.xml` 和本文档随 Sidebar 以 **Microsoft Public License (MS-PL)** 发布。完整文本见仓库根目录 `LICENSE` 和 `Licenses/MS-PL.txt`。
- `Mkho-Christmas-Snow-flake.ico` **不是 MS-PL 素材**。它来自 IconArchive 的 “Christmas Icons” 图标包，作者 MKho，页面标注的许可为：

  > Free for personal desktop use only. Commercial usage: Not allowed.

  来源：<https://iconarchive.com/show/christmas-icons-by-mkho/Snow-flake-icon.html>

  这意味着该图标不能随项目进行公开再分发，也不能用于商业用途。**如果要公开分发或商业分发这个主题，必须把它替换为自有图标或 CC0/兼容许可的图标**，并同步修改 `Theme.xaml` 的 `ThemeLogo`、`Sidebar.csproj` 的 Content 项以及 `NOTICE` 中的素材说明。仓库根目录的 `NOTICE` 记录了该图标的来源和限制。

- `SnowfallThemeProvider` 读取的注册表路径和系统事件属于 Windows 公共 API，不包含第三方代码。

---

## 11. 相关文档

- `Sidebar SDK/DOCs/styles_cn.html`：主题与样式参考，包含主题包结构、资源键、默认样式剖析。
- `Sidebar SDK/DOCs/doc_cn.html`：磁贴开发教程；理解磁贴如何消费主题资源后再改主题会更稳。
- `Sidebar SDK/DOCs/migration_cn.html`：旧主题/旧磁贴迁移说明。
- `NOTICE`：本仓库的第三方素材与 LongBar 归属声明。

---

## 12. 一句话总结

Snowfall 把“主题”从一张配色表扩展成了一个可执行的小型视觉层：**XAML 负责结构和绑定，程序集负责绘制和系统状态，Sidebar 负责把它们挂进真实视觉树**。把它当作范本，而不是必须遵循的规则；你可以只借用其中一层，也可以把三层都换成自己的实现。
