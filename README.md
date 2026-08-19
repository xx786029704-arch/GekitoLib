# GekitoLib（激类库）

Slay the Spire 2 激突要塞吧共享机制库 — 把多个角色 Mod 会重复实现的通用机制收敛到同一处。

基于 [BaseLib](https://github.com/Alchyr/BaseLib)，游戏为 Godot 4.5.1 (MegaDot) + C# (.NET 9)。

## 库内容

详见 [`docs/contents.md`](docs/contents.md)（API、实现要点、使用示例）。当前包含：焊接连锁、互斥姿态框架、临时属性能力（TemporaryStatPower）、沉底词条、尸爆术、多层护甲、色彩哲学家候选池、初始卡/初始遗物升级链、星图标换皮框架。

## 作为依赖使用

在你的 Mod 的 `.json` 清单中声明依赖：

```json
"dependencies": [
  {"id": "BaseLib", "min_version": "3.3.8"},
  {"id": "GekitoLib", "min_version": "0.3.0"}
]
```

在 csproj 中引用 dll（`<Private>false</Private>`，不随你的 Mod 重复分发）：

```xml
<Reference Include="GekitoLib">
  <HintPath>$(GameDir)/mods/GekitoLib/GekitoLib.dll</HintPath>
  <Private>false</Private>
</Reference>
```

⚠️ 你的 Mod 通过 HintPath 引用 `mods/GekitoLib/GekitoLib.dll`，因此**必须先构建 GekitoLib 再构建你自己的 Mod**。

## 构建

### 前置

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Godot 4.5.1 Mono](https://godotengine.org/)（MegaDot）
- [Slay the Spire 2](https://store.steampowered.com/app/2868840)（Steam）

### 环境配置

在项目根目录创建 `Directory.Build.props`（已 gitignore，各人自行配置）：

```xml
<Project>
  <PropertyGroup>
    <GameDir>你的游戏安装目录/Slay the Spire 2</GameDir>
    <MegaDotPath>你的 MegaDot 路径/Godot_v4.5.1-stable_mono_win64</MegaDotPath>
  </PropertyGroup>
</Project>
```

### 编译

```bash
dotnet build    # 仅代码改动（dll → 游戏 mods 目录）
dotnet publish  # 含资产改动（额外导出 .pck）
```

⚠️ 构建前关闭游戏，否则 dll 被进程锁定。

## 协作

### 准入原则

本库**只放通用机制** — 必须是「多个 Mod 都会需要、与具体角色/卡牌业务无关」的内容。带具体数值设计或只被单一 Mod 使用的内容不进本库。拿不准的先开 issue 讨论。

### 贡献流程

1. Fork 本仓库，创建 feature 分支
2. 实现前先查游戏/BaseLib 反编译源码中类似机制的命令用法
3. 代码风格：file-scoped namespace、Nullable 启用、标识符英文、注释中文
4. **同步更新 `docs/contents.md`**（新机制 = 完整条目：说明、成员表、实现要点、示例代码）
5. **同步本地化**：zhs + eng 双语言同时添加，key 前缀 `GEKITOLIB-`
6. 确保 `dotnet build` 通过（含资产改动则 `dotnet publish`）
7. **API 稳定性**：公开 API 变更 = 破坏性变更，在 PR 描述中注明影响范围

详细规范见 [AGENTS.md](AGENTS.md)。

### 本地化

中英文必须同步维护。风格对齐原版：

- 游戏机制名词 `[gold]…[/gold]`
- 附魔名 `[purple]…[/purple]`
- 数值 `[blue]{Var}[/blue]`

## 目录结构

```
GekitoLib.json          # Mod 清单（版本号、依赖）
GekitoLibCode/          # C# 源码
GekitoLib/              # 资产（打进 .pck）：图片 + 本地化 JSON
docs/contents.md        # 库内容文档（唯一权威）
```

## 许可

[MIT](LICENSE)

## 致谢

- [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2)
- [Alchyr/BaseLib](https://github.com/Alchyr/BaseLib)
- [Mega Crit](https://www.megacrit.com/)
