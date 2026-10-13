# 图像拼接工具（ImageStitcher）

基于 **WinForms + OpenCvSharp4** 的桌面图像拼接程序，支持 **SIFT / ORB** 两种特征算法，提供**自动拼接**与**手动拼接**两套工作方式。

- 自动拼接：算法自动估计单应矩阵，把两张图对齐并融合成全景图，同时给出匹配点可视化预览。
- 手动拼接：把两张图叠在一起，用鼠标拖动 / 旋转 / 调透明度人工对齐，所见即所得地导出结果。

---

## 目录

- [功能特性](#功能特性)
- [环境与依赖](#环境与依赖)
- [快速开始](#快速开始)
- [界面说明](#界面说明)
- [操作说明](#操作说明)
- [项目结构](#项目结构)
- [算法说明](#算法说明)
- [编程调用](#编程调用)
- [参数调优](#参数调优)
- [已验证行为](#已验证行为)

---

## 功能特性

**自动拼接**

- **SIFT**：匹配效果好、鲁棒性高，尺度/旋转不变，适合航拍等对精度要求高的场景。默认算法。
- **ORB**：速度快、特征点多，适合实时性要求较高的应用。
- 可任选一张图作为**基准图**，基准图决定全景图的坐标原点和方向。
- 后台线程执行，界面全程不卡；拼接期间进度条与状态提示可见。
- 结果分两个标签页展示：**拼接结果图** 与 **匹配点预览图**（外点红色、内点绿色，直观看出匹配质量）。
- 状态栏给出统计：特征点数、匹配数、RANSAC 内点数、耗时、重叠像素数。

**手动拼接**

- 左键拖动图像对齐，双击把图像置顶，中键平移画布，滚轮以光标为中心缩放。
- 明确指定**哪张图在上面**（顶层图像），避免依赖拖拽顺序猜测 z 序。
- **顶层半透明叠加** + 透明度滑块（0–100%），方便透出底图轮廓做精细对位。
- **旋转角度微调**（数值框 + 0.5° 步进按钮），应对两图有轻微角度差的情况。
- 导出时可选择**按当前透明度烘焙**（保留叠加效果）或导出**不透明结果**。

---

## 环境与依赖

| 项       | 版本 / 说明                                                         |
| -------- | ------------------------------------------------------------------- |
| 目标框架 | .NET Framework **4.6.2**（`net462`）                                |
| 界面     | WinForms（`UseWindowsForms=true`）                                  |
| 图像处理 | OpenCvSharp4 **4.11.0.20250507**                                    |
| 本机库   | OpenCvSharp4.runtime.win **4.11.0.20250507**                        |
| 平台     | **x64**（`PlatformTarget=x64`，不可改为 AnyCPU）                    |
| 语言版本 | C# 7.3                                                              |
| 构建     | .NET SDK（任意能构建 SDK 风格项目的版本）或 Visual Studio 2019/2022 |

运行环境需要 .NET Framework 4.6.2 或更高（已装 4.8 即可），以及 [Visual C++ 2015-2022 可再发行组件](https://aka.ms/vs/17/release/vc_redist.x64.exe)（`OpenCvSharpExtern.dll` 依赖它）。

---

## 快速开始

```powershell
# 构建
dotnet build .\ImageStitcher.sln -c Release

# 运行
.\ImageStitcher\bin\Release\net462\ImageStitcher.exe
```

也可以直接用 Visual Studio 打开 `ImageStitcher.sln` 后 F5。

> 本机库（`OpenCvSharpExtern.dll` 约 62 MB + ffmpeg 约 28 MB）由 csproj 在构建后自动从 NuGet 包拷到 exe 所在目录，无需手工处理，也不要手工把 `OpenCvSharpExtern.dll` 挪到别处。

---

## 界面说明

```
顶部工具区（高度 196 px，三组并排）
├─ 图像来源    加载图1 · 加载图2 · 交换 · 清空，以及图1 / 图2 的文件名与尺寸
├─ 自动拼接    算法(SIFT/ORB) · 基准图(图1/图2) · 显示匹配点 · 开始自动拼接 · 保存结果
└─ 手动拼接    顶层图像(图1/图2) · 旋转角度 + 左转/右转 · 透明度滑块
               进入手动模式 · 重置位置 · 适应窗口 · 导出不透明 · 导出结果

主区域（撑满剩余高度）
├─ 左侧 预览区（宽 236 px）   上方：图1 预览    下方：图2 预览
└─ 右侧 视图区
   ├─ 自动模式   TabControl，两个页签：拼接结果 / 匹配点预览
   └─ 手动模式   手动画布（两张图可拖动、旋转、调透明度）

底部 状态栏    状态文字 · 进度条（拼接时显示）· 模式指示（自动 / 手动）
```

界面尺寸：默认 1340×880，最小 1120×760。所有控件（含 `OpenFileDialog` / `SaveFileDialog`）均声明在 `MainForm.Designer.cs` 中，可在 Visual Studio 设计器里直接打开编辑。

---

## 操作说明

### 自动拼接

1. **加载图1 / 加载图2** 选择两张有重叠区域的图片。
2. 选择**算法**：精度优先选 `SIFT`，速度优先选 `ORB`。
3. 选择**基准图**：全景图将以这张图为基准（决定原点与方向）。
4. 勾选**显示匹配点**可在拼接完成后直接跳到匹配预览页。
5. 点击**开始自动拼接**，状态栏显示统计信息。
6. 点击**保存结果**导出全景图（PNG / JPG / BMP / TIFF）。

### 手动拼接

1. 加载两张图片后点击**进入手动模式**，视图区切换为手动画布（两张图按 30% 水平重叠预排布，并自动适应窗口）。
2. 对齐操作：

   | 操作     | 效果                              |
   | -------- | --------------------------------- |
   | 左键拖动 | 移动鼠标下方的图像                |
   | 双击图像 | 把该图像置顶                      |
   | 中键拖动 | 平移整个画布                      |
   | 滚轮     | 以光标位置为中心缩放（0.02×–32×） |

3. 用**顶层图像**下拉框明确指定哪张在上面；当前置顶图像会显示虚线边框。
4. 拖动**透明度**滑块让顶层半透明，透出底图以精确对位。
5. 用**旋转角度**数值框或**左转/右转**按钮（0.5° 步进）做角度微调。
6. **重置位置**恢复初始排布与角度，**适应窗口**重新缩放视图。
7. **导出结果**输出当前画布内容；**导出不透明**勾选框决定是否把顶层透明度一并烘焙进结果。

> 导出尺寸上限为单边 30000 px、总计 2 亿像素，超出会给出提示而不是耗尽内存。

---

## 项目结构

```
图像拼接/
├── ImageStitcher.sln
└── ImageStitcher/
    ├── ImageStitcher.csproj          项目文件（含 OpenCV 本机库拷贝目标）
    ├── Program.cs                    入口，STAThread + 全局异常兜底
    ├── MainForm.cs                   窗体逻辑与事件处理（不含控件创建）
    ├── MainForm.Designer.cs          全部控件声明与事件接线（设计器文件）
    ├── Controls/
    │   └── ManualStitchCanvas.cs     手动拼接画布（自绘控件，缩放/平移/旋转/合成）
    ├── Imaging/
    │   └── MatConverter.cs           Bitmap ⇄ Mat 互转（不依赖 OpenCvSharp4.Extensions）
    └── Stitching/
        ├── AutoStitcher.cs           自动拼接引擎（特征 → 匹配 → 单应 → 融合）
        ├── MatchPreviewRenderer.cs   匹配点并排可视化渲染
        ├── StitchAlgorithm.cs        算法枚举（Sift / Orb）
        ├── StitchOptions.cs          参数对象
        └── StitchResult.cs           结果对象（含统计信息与 Summary）
```

---

## 算法说明

### 自动拼接管线

```
预处理
 └─ MatConverter.ToMat ─▶ BGR ─ Cv2.CvtColor ─▶ 灰度图

灰度图 ─┬─ 1. 检测 + 描述    SIFT.Create / ORB.Create
        ├─ 2. 特征匹配       BFMatcher.KnnMatch(k=2) + Lowe 比值检验（阈值 0.75）
        ├─ 3. 单应估计       Cv2.FindHomography(RANSAC, 重投影阈值 3.0 px)
        ├─ 4. 画布规划       投影四角求包围盒 → 画布扩张（平移量折进 H 的平移列）
        ├─ 5. 透视变换       Cv2.WarpPerspective，待拼接图 → 基准图坐标系
        └─ 6. 羽化融合       距离变换加权，在 CV_32FC3 下加权求和后转回 CV_8UC3

输出 ─▶ 全景图
```

关键点：

- **匹配方向**：待拼接图的描述子是 query 集、基准图是 train 集，因此拟合出的单应矩阵直接把**待拼接图 → 基准图**坐标，无需再求逆。
- **失效即早退**：特征点 < 4、有效匹配 < 4、单应矩阵为空、内点 < 8 都会提前返回带原因的失败结果，不会产出畸形全景图。
- **羽化融合**：对两张图的覆盖掩码做距离变换，把距离按 `FeatherWidth` 截断后作为权重，在 `CV_32FC3` 下加权求和再转回 `CV_8UC3`。权重封顶使拼接缝宽度固定，不会随重叠面积变大而变糊。
- **除零防护**：权重分母在累加前用 `Scalar(1e-6)` 打底。这是必需的——`Cv2.Divide` 在分母为 0 时返回 `∞` 而不是 0，会把整张结果图污染成无效值。
- **画布保护**：全景图尺寸超过单边 30000 px 或 1.2 亿像素时拒绝构建，避免整数溢出与内存耗尽。

### 手动拼接

手动画布把两张图当作两个独立图层（各自的偏移、旋转、z 序），渲染时按 z 序依次绘入合成位图。顶层按透明度设置 `ColorMatrix.Matrix33` 后叠加。导出的合成图按图层包围盒求并集确定尺寸，与原图分辨率 1:1，不做额外缩放。

---

## 编程调用

自动拼接引擎可以脱离界面单独使用：

```csharp
using (Bitmap a = new Bitmap(@"left.jpg"))
using (Bitmap b = new Bitmap(@"right.jpg"))
{
    StitchOptions options = new StitchOptions();
    options.Algorithm = StitchAlgorithm.Sift;   // 或 StitchAlgorithm.Orb

    StitchResult result = AutoStitcher.Stitch(a, b, options);   // a 是基准图

    if (result.Success)
    {
        result.Image.Save(@"panorama.png", ImageFormat.Png);   // 调用方持有所有权
        Console.WriteLine(result.Summary);
        // SIFT：特征点 817/1071，匹配 421，内点 280，用时 229 ms
    }
    else
    {
        Console.WriteLine(result.Message);   // 失败原因，总是有值
    }

    if (result.Image != null) result.Image.Dispose();
    if (result.MatchPreview != null) result.MatchPreview.Dispose();
}
```

> **线程安全**：`Stitch` 会 `LockBits` 并绘制传入的位图，调用期间这两张图归本次调用独占。GDI+ 位图不是线程安全的，若界面仍在绘制同一批位图，会间歇性抛 `位图区域已被锁定`。调用方需要保留显示副本时，请先做私有副本再交给引擎——本程序的 `MainForm.Snapshot` 就是这么做的。

手动画布也可以单独使用（`ImageStitcher.Controls.ManualStitchCanvas`）：

```csharp
ManualStitchCanvas canvas = new ManualStitchCanvas();
canvas.SetImages(imageA, imageB);    // 不接管所有权
canvas.TopLayer = 1;                 // 图2 置顶
canvas.TopOpacityPercent = 40;       // 顶层 40% 不透明
canvas.SetRotation(1, 2.5f);         // 顶层旋转 2.5°
canvas.SetOffset(1, new PointF(120f, -8f));

using (Bitmap composite = canvas.RenderComposite(opaqueTopLayer: true))
{
    composite.Save(@"manual.png", ImageFormat.Png);
}

canvas.ClearImages();                // 释放引用后再自行 Dispose 位图
```

主要成员：`HasImages`、`TopLayer`、`TopOpacityPercent`、`TopRotation`、`Zoom`、`GetRotation/SetRotation`、`GetOffset/SetOffset`、`RotateTopBy`、`BringToFront`、`SwapTopLayer`、`SetImages`、`ClearImages`、`ResetPositions`、`ResetView`、`ZoomAt`、`CanvasToClient`、`ClientToCanvas`、`GetContentBounds`、`GetLayerBounds`、`HitTest`、`RenderComposite`，以及 `TopLayerChanged` 事件。

---

## 参数调优

全部参数集中在 `StitchOptions`：

| 参数                          | 默认值      | 说明                                        |
| ----------------------------- | ----------- | ------------------------------------------- |
| `Algorithm`                   | `Sift`      | 使用 SIFT 还是 ORB                          |
| `RatioThreshold`              | `0.75`      | Lowe 比值阈值，越小越严格、匹配越少但越可靠 |
| `RansacReprojectionThreshold` | `3.0`       | RANSAC 内点重投影阈值（像素）               |
| `SiftFeatureCount`            | `0`         | SIFT 特征数上限，`0` 表示不限制             |
| `OrbFeatureCount`             | `4000`      | ORB 特征数上限                              |
| `FeatherWidth`                | `48.0`      | 羽化带宽度（像素）                          |
| `MaxCanvasSide`               | `30000`     | 全景图单边尺寸上限                          |
| `MaxCanvasPixels`             | `120000000` | 全景图总像素上限                            |

调参建议：

- **匹配点太少**：把 `RatioThreshold` 放宽到 `0.8`，或换用 ORB（特征点更多）。
- **匹配点里外点太多**：把 `RatioThreshold` 收紧到 `0.65`，或降低 `RansacReprojectionThreshold`。
- **重叠区有轻微鬼影**：适当加大 `FeatherWidth`。
- **大图想更快**：改用 `Orb` 并降低 `OrbFeatureCount`。

---

## 已验证行为

以下数据来自自动化验证（用同一张大图切两块重叠区域：图 A 起点 (0,0)，图 B 起点 (520,120)，尺寸均为 800×600，因此正确的全景图必须是 **1320×720**）：

| 指标                    | SIFT       | ORB         |
| ----------------------- | ---------- | ----------- |
| 特征点（基准 / 待拼接） | 817 / 1071 | 3729 / 3900 |
| 匹配数                  | 421        | 600         |
| RANSAC 内点             | 280        | 413         |
| 耗时                    | 229 ms     | 227 ms      |
| 全景图尺寸              | 1320 × 721 | 1319 × 720  |
| 重叠像素                | 134400     | 134400      |

重叠区实测 134400 像素，与几何真值 `280 × 480 = 134400` **完全一致**；覆盖率 86.7% 也与几何预测值吻合，且重叠区内部无任何空洞像素、两图之外的角区无任何杂散像素。

> 表中耗时为单次测量值，仅用于说明两种算法的量级差别，实际耗时随图像内容与机器性能变化。SIFT 特征点少但精度高，ORB 特征点多、速度快。
