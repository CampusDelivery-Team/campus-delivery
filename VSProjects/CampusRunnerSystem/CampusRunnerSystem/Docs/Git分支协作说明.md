# Git 分支协作说明

## 推荐流程

1. 从主分支拉取最新代码。
2. 每位同学建立自己的功能分支，例如 `feature/user-pages`、`feature/task-service`。
3. 每次提交只包含一个清晰功能点。
4. 合并前先 build 项目，确认没有编译错误。
5. 合并时优先保护 `Program.cs`、`_Layout.cshtml`、公共 Helper、公共常量和数据库 SQL 文件。

## 冲突处理

- 不直接覆盖别人写的 Controller、Service、Repository。
- 冲突发生时先看双方代码目的，再手动合并。
- 如果涉及数据库表字段，先回到 `Database/campus_runner_oracle_schema_cn.sql` 核对真实字段。
