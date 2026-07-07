# 持久层 Repository

Repository 负责 SQL 和数据库读写。

Repository 通过 `Persistence/Oracle/OracleConnectionFactory.cs` 获取 Oracle 连接。

Repository 只负责读写数据库中的英文代码，不负责输出中文显示名称。
