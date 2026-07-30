# GekitoLib（激类库）

Slay the Spire 2 共享机制库 Mod。把多个角色 Mod 会重复实现的通用机制收敛到同一处实现、同一命名空间（`GekitoLib.*`），供其他 Mod 声明依赖后使用，避免反复造轮子。

基于 [BaseLib](https://github.com/Alchyr/BaseLib) 开发，游戏为 Godot 4.5.1 (MegaDot) + C# (.NET 9)。

## 当前机制

| 机制 | 命名空间 | 说明 |
|------|----------|------|
| **焊接连锁** | `GekitoLib.Weld` | 打出焊接牌时自动连锁打出牌组中所有焊接牌；提供共享关键词、连锁引擎、导管附魔 |
| **互斥姿态框架** | `GekitoLib.Stances` | 参考观者姿态系统：注册/切换/退出姿态 + 进出钩子 + 身体染色 |
| **临时属性能力** | `GekitoLib.Powers` | 能力存续期间给予等量属性（力量/敏捷等），移除时等额回收 |

详细 API、实现要点和使用示例见 [`docs/contents.md`](docs/contents.md)。

## 作为依赖使用

在你的 Mod 的 `.json` 清单中声明依赖：

```json
"dependencies": [
  {"id": "BaseLib", "min_version": "3.3.8"},
  {"id": "GekitoLib", "min_version": "0.1.0"}
]
```

在 csproj 中引用 dll（`<Private>false</Private>`，不随你的 Mod 重复分发）：

```xml
<Reference Include="GekitoLib">
  <HintPath>$(GameDir)/mods/GekitoLib/GekitoLib.dll</HintPath>
  <Private>false</Private>
</Reference>
```

⚠️ **构建顺序**：你的 Mod 通过 HintPath 引用 `mods/GekitoLib/GekitoLib.dll`，因此必须先构建 GekitoLib 再构建你自己的 Mod。

## 构建

### 前置条件

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Godot 4.5.1 Mono](https://godotengine.org/)（MegaDot 版本）
- [Slay the Spire 2](https://store.steampowered.com/app/2868840)（Steam 安装）

### 配置

在项目根目录创建 `Directory.Build.props`（已 gitignore，不入库）：

```xml
<Project>
  <PropertyGroup>
    <GameDir>E:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2</GameDir>
    <MegaDotPath>C:/megadot/Godot_v4.5.1-stable_mono_win64</MegaDotPath>
  </PropertyGroup>
</Project>
```

### 编译

```bash
# 仅代码改动（编译 dll 并复制到游戏 mods 目录）
dotnet build

# 含资产改动（本地化/图片等，额外导出 .pck）
dotnet publish
```

⚠️ 构建前请关闭游戏（否则 dll 被进程锁定，复制失败 MSB3021）。

## 本地化

本项目同时维护中文（zhs）与英文（eng）本地化，所有 key 以 `GEKITOLIB-` 为前缀。风格对齐原版：游戏机制名词 `[gold]…[/gold]`，附魔名 `[purple]…[/purple]`，数值 `[blue]{Var}[/blue]`。

## 协作指南

欢迎贡献！请遵循以下流程：

1. Fork 本仓库并创建 feature 分支
2. **通用性自检**：拟新增的机制须是「多个 Mod 都会需要、与具体角色/卡牌业务无关」的通用机制。带具体数值设计或只被单一 Mod 使用的内容不进本库
3. 实现前先查游戏/BaseLib 反编译源码中类似机制的命令用法，不凭空写 API
4. 代码遵循项目风格（file-scoped namespace、Nullable 启用、标识符英文、注释中文）
5. 同步更新 `docs/contents.md`（新增机制时补完整条目）
6. 同步更新中英文本地化（zhs + eng）
7. **API 稳定性**：公开 API 变更属于破坏性变更，需在 PR 中注明影响范围
8. 提交前确保 `dotnet build` 通过，含资产改动则 `dotnet publish` 验证

详细规范见 [AGENTS.md](AGENTS.md)。

## 目录结构

```
GekitoLib.json          # Mod 清单
GekitoLibCode/          # C# 源码
  MainFile.cs           # Mod 入口
  Weld/                 # 焊接连锁
  Stances/              # 互斥姿态框架
  Powers/               # 临时属性能力
  Enchantments/         # 导管附魔
GekitoLib/              # 资产（打进 .pck）
  images/               # 图标
  localization/         # 本地化 JSON（zhs + eng）
docs/contents.md        # 内容文档
```

## 许可

待定。

## 致谢

- [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — Mod 模板
- [Alchyr/BaseLib](https://github.com/Alchyr/BaseLib) — 社区核心库
- [Mega Crit](https://www.megacrit.com/) — Slay the Spire 2
