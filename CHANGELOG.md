# Changelog

## 1.1.0

### Fixed

- `Compress` 对空输入 / 1 字节输入直接 `IndexOutOfRangeException`
- match 结束于输入末尾时，match 后的 rehash 无条件读取 `input[index + 2]`，越界读取
- `inputLength` 不校验（大于 `inputBytes.Length` 时越界读、或把无关字节压进结果；为负时无限循环）
- `ref byte[] outputBuffer` 参数传入 `null` 时有更良好的异常信息（`ArgumentNullException`）
- `Decompress` 不校验输入边界，损坏数据导致 `IndexOutOfRangeException`；现在抛 `ArgumentException` 并带消息
- 解压缓冲区按 ×2 无上限增长，恶意/损坏数据会导致 OOM；现在封顶 1 GB（`OutOfMemoryException`）
- 空输入 / 1 字节输入可正常往返（之前直接崩溃）
- `Compress(byte[], int)` / `Decompress(byte[], int)` 输入越界现在抛 `ArgumentOutOfRangeException`

### Added

- 完整的单元测试（`Tests/Editor`，NUnit，Unity Test Runner / EditMode 运行），覆盖往返一致性、边界尺寸、缓冲区复用、损坏数据、线程安全等

### Changed

- `feature-upm` 分支为 UPM 包布局（上游 master 已包含在历史中）
