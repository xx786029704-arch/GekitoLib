# AGENTS.md — GekitoLib（激类库，StS2 共享机制库）

## 项目概述

Slay the Spire 2 共享机制库 Mod，基于社区库 **BaseLib** 开发（游戏为 Godot 4.5.1 + C# / .NET 9）。把多个角色 Mod 会重复实现的通用机制收敛到同一处实现、同一命名空间（`GekitoLib.*`），供其他 Mod 声明依赖后使用，避免反复造轮子。

- Mod ID：`GekitoLib`（**不可更改**，决定加载文件名与 `res://GekitoLib` 资源路径）
- 游戏路径在 `Directory.Build.props`（本机配置，不入库）中设置；Sts2PathDiscovery.props 会自动探测 Steam 安装位置
- **当前库内容见 `docs/contents.md`**（机制清单、API、实现要点、使用示例）。新增/修改机制时必须同步更新该文档

## 工作原则

1. **只放通用机制**：本库只收纳「多个 Mod 都会需要、与具体角色/卡牌业务无关」的机制。任何带具体数值设计、具体卡牌/遗物/角色耦合的内容都不进本库。
2. **API 稳定性优先**：本库被其他 Mod 编译期引用，公开 API（命名空间、类名、方法签名）变更属于破坏性变更，改之前先评估对所有使用方的影响，并在 docs/contents.md 中注明。
3. **先查再写**：实现具体效果前，先想原版游戏中什么内容与之类似，然后查游戏/BaseLib 的反编译源码参考其命令用法，不要凭记忆写 API。
4. **行为一律用 Commands 组合**（`PowerCmd.Apply` / `CardCmd.AutoPlay` / `CardPileCmd` 等），不要直接改数据。
5. **注释用中文，标识符用英文**；file-scoped namespace、Nullable 启用，遵循既有风格。

## 目录结构

```
GekitoLib.json          # Mod 清单（版本号、依赖在此更新）
GekitoLibCode/          # C# 代码
  MainFile.cs           # Mod 入口（ModInitializer + Harmony PatchAll）
  Weld/                 # 焊接连锁：共享关键词 + 连锁引擎
  Stances/              # 互斥姿态框架（注册表 + 切换命令 + 进出钩子 + 染色）
  Powers/               # 通用能力基类（临时属性能力）
  Enchantments/         # 通用附魔（导管）
GekitoLib/              # 资产（打进 .pck）
  images/enchantments/  # 附魔图标
  localization/eng|zhs/ # 本地化 JSON（card_keywords / enchantments）
docs/contents.md        # 库内容文档（唯一权威内容清单）
```

## 新增内容的标准流程

往库里加新机制/新内容时，按以下清单执行（缺一不可）：

1. **通用性自检**：先确认该内容是「多个 Mod 都会需要、与具体角色/卡牌业务无关」的机制。只被单一 Mod 使用、或带具体数值设计的内容不进本库。
2. **查参考实现**：先找原版游戏/BaseLib 中的类似机制，照其命令用法实现；不要凭记忆写 API。
3. **同步 `docs/contents.md`**：为新机制补充完整条目——机制说明、成员表（类/方法/本地化 key）、实现要点（踩坑与顺序依赖）、使用示例代码。文档与代码同提交，不允许只改代码不改文档。
4. **本地化双语言**：zhs 与 eng 条目同时添加，key 前缀 `GEKITOLIB-`；缺条目 ModAnalyzers 会编译报错。
5. **资产随 Publish 验证**：新增图片/本地化等资产后必须 `dotnet publish`，并验证 pck 内确实包含新文件（二进制搜 key/文件名），不能只信导出日志。
6. **不破坏现有 API**：新内容只做增量；若必须改动既有公开签名，先按「API 稳定性优先」原则评估并在 contents.md 注明破坏性变更。
7. **回归验证使用方**：构建本库后，重新构建至少一个使用方 Mod 确认编译不破坏；有行为变更时说明需要游戏内实测的点。
8. **版本号**：内容新增后手动升 `GekitoLib.json` 的 `version`（新增机制升 minor，破坏性变更升 major 并在发布说明标注）。

## 构建与发布

- **Build**（仅代码改动）：`dotnet build`，编译 dll 并自动复制 dll/pdb/json 到游戏 `mods/GekitoLib/`。
- **Publish**（改了本地化/图片等任何非代码资产）：`dotnet publish`，经 MegaDot 导出 .pck 并复制三件套。**资产改动必须 Publish 才生效**。
- 构建前必须先关闭游戏（SlayTheSpire2 进程），否则 dll 被锁定导致复制失败（MSB3021）。
- **使用方 Mod 通过 HintPath 引用 `mods/GekitoLib/GekitoLib.dll`，改完本库后必须重新构建使用方 Mod 验证不破坏编译。**
- Publish 后验证 pck 内容（资产是否真的进包），别只看导出日志成功。
- 发布包 = `GekitoLib.dll` + `GekitoLib.pck` + `GekitoLib.json` 三件套；版本号在 `GekitoLib.json` 的 `version` 字段，发布前手动升。

## 本地化

- 中文（zhs）与英文（eng）必须同步维护，新增条目两套都写。
- key 前缀固定为 `GEKITOLIB-`（BaseLib 按 Mod ID 生成）。关键词/附魔的 key 是其他 Mod 的卡牌描述里也会引用的公共资源，改名即破坏性变更。
- BBCode 风格对齐原版：游戏机制名词 `[gold]…[/gold]`，附魔名 `[purple]…[/purple]`，数值 `[blue]{Var}[/blue]`。

## 测试与调试

- 游戏内按 `` ` `` / `~` 开 dev console；日志文件：`%appdata%/SlayTheSpire2/logs/godot.log`。
- 自己的日志：`MainFile.Logger.Info(...)`。
- 本库无独立可运行内容，验证需借助任一使用方 Mod 做集成测试。
