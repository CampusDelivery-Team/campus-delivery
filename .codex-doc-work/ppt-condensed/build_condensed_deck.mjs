import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { FileBlob, PresentationFile } from '@oai/artifact-tool';

const workspaceDir = 'D:\\delivery-backend';
const sourcePath = path.join(workspaceDir, 'docs', '校园综合跑腿与代取服务管理系统-课程设计答辩.pptx');
const buildDir = path.join(workspaceDir, '.codex-doc-work', 'ppt-condensed');
const outputDir = path.join(workspaceDir, 'docs');
const finalPath = path.join(outputDir, '校园综合跑腿与代取服务管理系统-课程设计答辩精简版.pptx');
const erImagePath = path.join(workspaceDir, '.codex-doc-work', 'dbdoc-unpacked', 'word', 'media', 'image10.png');
const skillDir = 'C:\\Users\\liuxc\\.codex\\plugins\\cache\\openai-primary-runtime\\presentations\\26.905.11957\\skills\\presentations';
const pythonExecutable = 'C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\python\\python.exe';
process.env.RUNTIME_NODE = 'C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\bin\\node.exe';
process.env.RUNTIME_NODE_MODULES = 'C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules';

await fs.mkdir(buildDir, { recursive: true });
await fs.mkdir(outputDir, { recursive: true });
try {
  await fs.access(finalPath);
  throw new Error(`目标文件已存在：${finalPath}`);
} catch (error) {
  if (error.code !== 'ENOENT') throw error;
}

const presentation = await PresentationFile.importPptx(await FileBlob.load(sourcePath));
const keepOriginalSlides = new Set([1, 2, 4, 6, 7, 8, 9, 11, 12, 13, 14, 16, 17, 18, 19, 42, 44]);
for (let index = presentation.slides.items.length - 1; index >= 0; index -= 1) {
  if (!keepOriginalSlides.has(index + 1)) presentation.slides.getItem(index).delete();
}
if (presentation.slides.items.length !== 17) throw new Error(`精简后应为 17 页，实际为 ${presentation.slides.items.length} 页`);

async function snapshot() {
  const result = await presentation.inspect({ kind: 'slide,shape,textbox,image,table,layout', maxChars: 180000 });
  return result.ndjson.split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line));
}
let records = await snapshot();

function recordByName(slideNumber, name) {
  const matches = records.filter(record => record.slide === slideNumber && record.name === name && typeof record.id === 'string');
  if (matches.length !== 1) throw new Error(`第 ${slideNumber} 页名称 ${name} 命中 ${matches.length} 个对象`);
  return matches[0];
}
function recordByText(slideNumber, text) {
  const matches = records.filter(record => record.slide === slideNumber && record.text === text && typeof record.id === 'string');
  if (matches.length !== 1) throw new Error(`第 ${slideNumber} 页文本“${text}”命中 ${matches.length} 个对象`);
  return matches[0];
}
function setByName(slideNumber, name, value) {
  const record = recordByName(slideNumber, name);
  presentation.resolve(record.id).text = value;
}
function setByExactText(slideNumber, oldText, value) {
  const record = recordByText(slideNumber, oldText);
  presentation.resolve(record.id).text = value;
}

// 目录：移除团队分工主题，改为测试与答辩问答。
setByName(2, 'toc-d1', '业务背景 · 技术架构 · 数据库驱动分层');
setByName(2, 'toc-d2', '五类功能模块 · 核心业务状态机 · 权限与售后');
setByName(2, 'toc-d3', '真实物理模型 · 24 表关系 · 索引与完整性约束');
setByName(2, 'toc-d4', '真实界面 · 参数化 SQL · 事务与行锁');
setByExactText(2, '团队分工', '测试与总结');
setByExactText(2, '角色职责 · 核心实现 · 产出与验证', '自动化测试 · 结构门禁 · Oracle 端到端验证');
setByExactText(2, '测试与总结', '答辩问答');
setByExactText(2, '功能与并发测试 · 成果总结 · 不足与展望', '设计依据 · 关键约束 · 核心业务逻辑');

// 项目背景与架构均按当前仓库口径表述。
setByName(3, 'hd-title', '项目背景：校园跑腿需要统一履约与数据管理');
setByName(3, 'bg-intro', '外卖分发 / 快递代取 / 私人跑腿集中管理 · 状态可追踪 · 资金与售后可审计');
setByName(3, 'pain1', '发布入口分散\n群聊与口头约定难以沉淀任务、价格、地址和履约状态');
setByName(3, 'pain2', '履约过程难追踪\n接单、取货、配送、确认收货缺少统一状态与日志');
setByName(3, 'pain3', '资金与责任难核验\n支付、退款、评价、投诉和结算缺少一致的数据依据');
setByName(4, 'layer5-t', '数据库层　Oracle 19c\n24 张业务表 · 23 个显式索引 · 31 个外键 · 30 个 CHECK 约束');

// 章节页总数调整为五个正文部分。
setByName(5, 'div-progress', 'SECTION 02 / 05');
setByName(8, 'div-progress', 'SECTION 03 / 05');
setByName(13, 'div-progress', 'SECTION 04 / 05');
setByName(13, 'div-sub', '真实界面 · 数据库对象调用 · 事务与行锁');

// 第 9 页使用数据库设计文档中由当前 24 表结构生成的真实物理模型。
const erSlide = presentation.slides.getItem(8);
erSlide.shapes.deleteAll();
erSlide.background.fill = '#F8F4EC';
const addText = (name, text, position, style) => {
  const shape = erSlide.shapes.add({
    geometry: 'textbox', name, position, fill: 'none', line: { fill: 'none', width: 0 },
  });
  shape.text = text;
  shape.text.style = style;
  return shape;
};
erSlide.shapes.add({ geometry: 'rect', name: 'hd-tick', position: { left: 53.33, top: 45.33, width: 6.67, height: 32 }, fill: '#E8551D', line: { fill: 'none', width: 0 } });
addText('hd-title', '总体 E-R 图（真实物理模型）', { left: 74.67, top: 40, width: 880, height: 45.33 }, { typeface: 'MiSans', fontSize: 33.33, bold: true, color: '#23272E', autoFit: 'none' });
addText('hd-sec', '03 · 数据库设计', { left: 933.33, top: 56, width: 293.33, height: 18.67 }, { typeface: 'MiSans', fontSize: 14.67, color: '#77716A', alignment: 'right', autoFit: 'none' });
erSlide.shapes.add({ geometry: 'rect', name: 'hd-line', position: { left: 53.33, top: 101.33, width: 1173.33, height: 1.33 }, fill: '#DCD5C6', line: { fill: 'none', width: 0 } });
addText('er-note', '依据 campus_runner_oracle_schema.sql 生成，完整覆盖 24 张业务表及关联表', { left: 53.33, top: 112, width: 1173.33, height: 24 }, { typeface: 'MiSans', fontSize: 14.67, color: '#77716A', alignment: 'center', autoFit: 'none' });
const erBytes = await fs.readFile(erImagePath);
erSlide.images.add({ blob: erBytes, contentType: 'image/png', alt: 'Oracle 24张业务表真实物理模型上半部分', fit: 'cover', crop: { left: 0, top: 0, right: 0, bottom: 0.49 }, position: { left: 53.33, top: 143, width: 568, height: 510 } });
erSlide.images.add({ blob: erBytes, contentType: 'image/png', alt: 'Oracle 24张业务表真实物理模型下半部分', fit: 'cover', crop: { left: 0, top: 0.49, right: 0, bottom: 0 }, position: { left: 658.67, top: 143, width: 568, height: 510 } });
erSlide.shapes.add({ geometry: 'rect', name: 'ft-line', position: { left: 53.33, top: 682.67, width: 1173.33, height: 1.33 }, fill: '#DCD5C6', line: { fill: 'none', width: 0 } });
addText('ft-brand', '济跑跑 JIRUN · 校园综合跑腿与代取服务管理系统', { left: 53.33, top: 693.33, width: 533.33, height: 18.67 }, { typeface: 'MiSans', fontSize: 13.33, color: '#77716A', autoFit: 'none' });
addText('ft-page', '9 / 17', { left: 1093.33, top: 693.33, width: 133.33, height: 18.67 }, { typeface: 'MiSans', fontSize: 13.33, color: '#77716A', alignment: 'right', autoFit: 'none' });
erSlide.speakerNotes.textFrame.setText('数据来源：database/oracle/campus_runner_oracle_schema.sql；图源：校园综合跑腿与代取服务管理系统-数据库设计文档.docx 中的真实物理模型。');

records = await snapshot();

// 逻辑结构与核心表描述。
setByName(10, 'hd-title', '关系模式与核心表');
setByName(10, 'schema-note', '当前结构共 24 张业务表。主实体使用单列主键，任务明细使用复合主键，service_node_rules 等关联表表达多对多关系。');
const logicTableRecord = records.find(record => record.slide === 10 && record.kind === 'table');
if (!logicTableRecord) throw new Error('未找到第 10 页逻辑结构表');
const logicTable = presentation.resolve(logicTableRecord.id);
logicTable.cells.set(4, 0, '任务明细（外卖 / 快递 / 私人）');

setByName(11, 'hd-title', 'tasks 任务主表（真实 DDL）');
setByName(11, 'note1', '主单与明细分离\n任务公共字段写入 tasks，外卖分发、快递代取、私人跑腿分别写入三张明细表');

// 数据库对象与约束：替换旧版“未使用存储过程”的错误表述。
setByName(12, 'hd-title', '数据库对象：结构、约束与并发控制');
setByName(12, 'q1-title', '结构规模 SCHEMA');
setByName(12, 'q1-body', '24 张业务表 · 24 个主键 · 31 个外键\n23 个显式索引覆盖任务、接派、支付、退款与结算查询\n主表、弱实体明细和关联表共同支撑业务闭环');
setByName(12, 'q2-title', '完整性约束 CONSTRAINT');
setByName(12, 'q2-body', '30 个 CHECK 约束限定状态、金额与枚举取值\n组合外键保证任务的用户地址和服务节点组合真实存在\n唯一约束落实单默认地址、一任务一评价与支付流水号唯一');
setByName(12, 'q3-body', '状态更新与日志写入在同一事务内提交\n支付、退款和结算跨表写入失败时整体回滚\n14 处 FOR UPDATE 语句保护抢单、审核与资金等热点数据');
setByName(12, 'q4-title', '视图、过程与函数');
setByName(12, 'q4-body', '4 个业务视图支撑任务、支付退款、跑腿员绩效与结算查询\n4 个存储过程处理账号、默认地址、审核和原子接单\n3 个函数负责价格、接单资格与服务节点规则，后端均有真实调用');
setByName(12, 'obj-concl', '真实对象规模：24 表 · 4 视图 · 4 过程 · 3 函数');

// 系统实现与数据库映射。
setByName(14, 'ph1-cap', '普通用户端　发布三类任务、跟踪状态、确认收货付款、评价与售后\n对应：tasks · payments · reviews · refunds · complaints');
setByName(14, 'ph2-cap', '跑腿员与管理员端　任务大厅抢单、派单重派、配送状态、结算审计报表\n对应：assign_records · task_status_logs · settlements · audit_logs · reports');

setByName(15, 'hd-title', '原子接单：存储过程与事务边界');
const code = `-- database-enhancement/03_procedures.sql\n+CREATE OR REPLACE PROCEDURE sp_accept_task_atomic (...) AS\n+BEGIN\n+  SELECT task_status, publisher_user_id\n+    INTO v_task_status, v_publisher_user_id\n+    FROM tasks WHERE task_id = p_task_id FOR UPDATE;\n+\n+  IF v_task_status <> 'WAITING' THEN\n+    p_result_code := 'TASK_NOT_WAITING'; RETURN;\n+  END IF;\n+\n+  UPDATE tasks SET task_status = 'ASSIGNED'\n+   WHERE task_id = p_task_id AND task_status = 'WAITING';\n+  UPDATE runners SET work_status = 'BUSY'\n+   WHERE runner_id = p_runner_id;\n+  INSERT INTO assign_records (...) RETURNING record_id INTO p_record_id;\n+  INSERT INTO task_status_logs (...);\n+  p_result_code := 'SUCCESS';\n+END;\n+-- AssignRepository 负责统一 COMMIT / ROLLBACK`;
const codeRecord = recordByName(15, 'code-text');
const codeShape = presentation.resolve(codeRecord.id);
codeShape.text = code;
codeShape.text.style = { typeface: 'JetBrains Mono', fontSize: 12.4, color: '#F8F4EC', autoFit: 'shrinkText' };
setByName(15, 'anno1', '存储过程统一入口\nsp_accept_task_atomic 完成资格、操作者和任务状态校验');
setByName(15, 'anno2', '行锁防并发抢单\nFOR UPDATE 锁定任务与账号数据，后到事务读取最新状态');
setByName(15, 'anno3', '事务由调用方控制\n过程内部不提交，Repository 统一执行 COMMIT 或 ROLLBACK');
setByName(15, 'anno-note', '仓库证据：AssignRepository.cs + 03_procedures.sql');

// 测试页使用当前仓库的实际测试结果与门禁数字。
setByName(16, 'hd-sec', '05 · 测试与总结');
setByName(16, 'test-env', '测试环境：Oracle 19c · .NET 9 · xUnit；60 项自动化测试全部通过，并保留 Oracle 端到端验证脚本与截图');
setByName(16, 'test-concl', '结构门禁：24 张表 · 47/47 POST 防伪校验 · 14 处 FOR UPDATE · 分层边界检查通过');
const testTableRecord = records.find(record => record.slide === 16 && record.kind === 'table');
if (!testTableRecord) throw new Error('未找到第 16 页测试表');
const testTable = presentation.resolve(testTableRecord.id);
testTable.cells.set(1, 1, '用户发布任务并确认付款');
testTable.cells.set(1, 2, '事务提交，tasks 与 payments 状态一致');
testTable.cells.set(3, 1, '已付款任务申请退款');
testTable.cells.set(3, 2, 'refunds 记录与任务售后状态一致');
testTable.cells.set(4, 1, '管理员派单与重派');
testTable.cells.set(4, 2, '新增接派记录，任务状态与日志同步更新');
testTable.cells.set(4, 3, '事务与接派留痕');

// 统一新页码。
records = await snapshot();
for (let slideNumber = 3; slideNumber <= 16; slideNumber += 1) {
  const page = records.find(record => record.slide === slideNumber && ['ft-page', 'div-page'].includes(record.name));
  if (!page) throw new Error(`第 ${slideNumber} 页缺少页码对象`);
  presentation.resolve(page.id).text = `${slideNumber} / 17`;
}
const finalPage = records.find(record => record.slide === 17 && record.name === 'th-page');
if (!finalPage) throw new Error('结尾页缺少页码对象');
presentation.resolve(finalPage.id).text = '17 / 17';

// 明确确认团队分工板块已完全删除。
const teamCheck = await presentation.inspect({ kind: 'slide,shape,textbox,table', search: '团队分工', maxChars: 4000 });
if (teamCheck.ndjson.trim()) throw new Error(`仍存在团队分工内容：${teamCheck.ndjson}`);

const after = await presentation.inspect({ kind: 'slide,shape,textbox,image,table,layout', maxChars: 30000 });
await fs.writeFile(path.join(buildDir, 'condensed-inspect.ndjson'), after.ndjson, 'utf8');

const sha256 = crypto.createHash('sha256').update(await fs.readFile(sourcePath)).digest('hex');
const { finalizePresentation } = await import(pathToFileURL(path.join(skillDir, 'container_tools', 'artifact_tool_utils.mjs')).href);
const candidatePath = path.join(buildDir, 'candidate-condensed.pptx');
await (await PresentationFile.exportPptx(presentation)).save(candidatePath);
const result = await finalizePresentation({
  workspaceDir,
  candidatePath,
  finalPath,
  explicitTotalSlideCount: 17,
  requiredNativeTableOwnerSlides: [10, 11, 16],
  requiredNativeChartOwnerSlides: [],
  pythonExecutable,
  integrityValidatorPath: path.join(skillDir, 'container_tools', 'inspect_presentation_package_integrity.py'),
  layoutValidatorPath: path.join(skillDir, 'container_tools', 'inspect_presentation_layout_geometry.py'),
  layoutArgs: ['--expected-slide-size-emu', '12192000,6858000', '--validate-bullet-geometry', '--validate-heading-fit', '--require-native-table-slide', '10', '--require-native-table-slide', '11', '--require-native-table-slide', '16'],
  fontPolicy: { basis: 'reference', families: ['MiSans', 'JetBrains Mono', 'Times New Roman'], referencePath: sourcePath, referenceSha256: sha256 },
  verifyArtifactToolImport: true,
  receiptPath: path.join(buildDir, 'condensed.validation.json'),
});

console.log(JSON.stringify({ finalPath, slideCount: presentation.slides.items.length, result }, null, 2));
