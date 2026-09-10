import fs from "node:fs/promises";
import path from "node:path";
import crypto from "node:crypto";
import { pathToFileURL } from "node:url";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const workspaceDir = "D:\\delivery-backend";
const sourcePath = path.join(workspaceDir, "济跑跑工程档案 · 课程设计答辩.pptx");
const buildDir = path.join(workspaceDir, ".codex-doc-work", "ppt-name-update");
const outputDir = path.join(workspaceDir, "docs");
const finalPath = path.join(outputDir, "校园综合跑腿与代取服务管理系统-课程设计答辩.pptx");
const skillDir = "C:\\Users\\liuxc\\.codex\\plugins\\cache\\openai-primary-runtime\\presentations\\26.905.11957\\skills\\presentations";
const pythonExecutable = "C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\python\\python.exe";
process.env.RUNTIME_NODE = "C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\bin\\node.exe";
process.env.RUNTIME_NODE_MODULES = "C:\\Users\\liuxc\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules";
const oldName = "校园中转分发与跑腿服务管理系统";
const newName = "校园综合跑腿与代取服务管理系统";
const oldEnglishName = "Campus Relay Distribution & Errand ServiceManagement System";
const newEnglishName = "Campus Comprehensive Errand and Pickup Service Management System";

await fs.mkdir(buildDir, { recursive: true });
await fs.mkdir(outputDir, { recursive: true });
try {
  await fs.access(finalPath);
  throw new Error(`目标文件已存在：${finalPath}`);
} catch (error) {
  if (error.code !== "ENOENT") throw error;
}

const presentation = await PresentationFile.importPptx(await FileBlob.load(sourcePath));
const before = await presentation.inspect({
  kind: "slide,textbox,shape,layout",
  search: oldName,
  maxChars: 30000,
});
await fs.writeFile(path.join(buildDir, "before-inspect.ndjson"), before.ndjson, "utf8");

const records = before.ndjson
  .split(/\r?\n/)
  .filter(Boolean)
  .map((line) => JSON.parse(line));
const targetIds = [...new Set(records
  .filter((record) => typeof record.id === "string" && record.id.startsWith("sh/") &&
    `${record.text ?? record.textPreview ?? ""}`.includes(oldName))
  .map((record) => record.id))];

if (targetIds.length !== 36) {
  throw new Error(`应找到 36 个含旧名称的文本对象，实际找到 ${targetIds.length} 个`);
}

for (const id of targetIds) {
  const target = presentation.resolve(id);
  target.text.replace(oldName, newName);
}

const englishBefore = await presentation.inspect({
  kind: "textbox,shape",
  search: "Campus Relay Distribution",
  maxChars: 4000,
});
const englishRecords = englishBefore.ndjson
  .split(/\r?\n/)
  .filter(Boolean)
  .map((line) => JSON.parse(line));
const englishTargetIds = [...new Set(englishRecords
  .filter((record) => typeof record.id === "string" && record.id.startsWith("sh/") &&
    `${record.text ?? record.textPreview ?? ""}`.includes("Campus Relay Distribution"))
  .map((record) => record.id))];
if (englishTargetIds.length !== 1) {
  throw new Error(`应找到 1 个英文旧名称文本对象，实际找到 ${englishTargetIds.length} 个`);
}
presentation.resolve(englishTargetIds[0]).text = newEnglishName;

const after = await presentation.inspect({
  kind: "slide,textbox,shape,layout",
  search: oldName,
  maxChars: 30000,
});
await fs.writeFile(path.join(buildDir, "after-old-name-inspect.ndjson"), after.ndjson, "utf8");
const newHits = await presentation.inspect({
  kind: "slide,textbox,shape,layout",
  search: newName,
  maxChars: 30000,
});
await fs.writeFile(path.join(buildDir, "after-new-name-inspect.ndjson"), newHits.ndjson, "utf8");

const sha256 = crypto.createHash("sha256").update(await fs.readFile(sourcePath)).digest("hex");
const { finalizePresentation } = await import(pathToFileURL(
  path.join(skillDir, "container_tools", "artifact_tool_utils.mjs"),
).href);
const candidatePath = path.join(buildDir, "candidate.pptx");
await (await PresentationFile.exportPptx(presentation)).save(candidatePath);
const result = await finalizePresentation({
  workspaceDir,
  candidatePath,
  finalPath,
  explicitTotalSlideCount: 44,
  requiredNativeTableOwnerSlides: [],
  requiredNativeChartOwnerSlides: [],
  pythonExecutable,
  integrityValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_package_integrity.py"),
  layoutValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_layout_geometry.py"),
  layoutArgs: [
    "--expected-slide-size-emu", "12192000,6858000",
    "--validate-bullet-geometry",
    "--validate-heading-fit",
  ],
  fontPolicy: {
    basis: "reference",
    families: ["MiSans", "JetBrains Mono", "Times New Roman"],
    referencePath: sourcePath,
    referenceSha256: sha256,
  },
  verifyArtifactToolImport: true,
  receiptPath: path.join(buildDir, "final.validation.json"),
});

console.log(JSON.stringify({ targetCount: targetIds.length, englishTargetCount: englishTargetIds.length, finalPath, result }, null, 2));
