const { chromium } = require(process.env.PLAYWRIGHT_CORE_PATH);
const path = require("path");

for (const name of [
  "TEST_BASE_URL", "TEST_ADMIN_USERNAME", "TEST_ADMIN_PASSWORD",
  "TEST_USER_USERNAME", "TEST_USER_PHONE", "TEST_USER_PASSWORD",
  "TEST_RUN_ID", "TEST_OUTPUT_ROOT",
]) {
  if (!process.env[name]) throw new Error(`Missing environment variable: ${name}`);
}

const baseUrl = process.env.TEST_BASE_URL;
const runId = process.env.TEST_RUN_ID;
const nodeName = `${runId}测试节点`;
const editedNodeName = `${nodeName}-已修改`;
const serviceName = `${runId}测试服务`;
const output = (name) => path.join(process.env.TEST_OUTPUT_ROOT, name);
const edgePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

async function login(page, username, password) {
  await page.goto(`${baseUrl}/Auth/Login`, { waitUntil: "networkidle" });
  await page.locator("#Username").fill(username);
  await page.locator("#Password").fill(password);
  await Promise.all([
    page.waitForURL((url) => !url.pathname.startsWith("/Auth/Login"), { timeout: 15000 }),
    page.locator('form[action="/Auth/Login"] button[type="submit"]').click(),
  ]);
}

async function submitAndWait(page, button) {
  await Promise.all([
    page.waitForLoadState("domcontentloaded"),
    button.click(),
  ]);
  await page.waitForLoadState("networkidle");
}

async function selectContaining(page, selector, text) {
  const value = await page.locator(selector).locator("option").evaluateAll(
    (options, expected) => options.find((option) => option.textContent.includes(expected))?.value,
    text,
  );
  if (!value) throw new Error(`No option containing '${text}' in ${selector}`);
  await page.locator(selector).selectOption(value);
  return value;
}

(async () => {
  const browser = await chromium.launch({ executablePath: edgePath, headless: true });
  const adminContext = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const userContext = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const admin = await adminContext.newPage();
  const user = await userContext.newPage();

  await login(admin, process.env.TEST_ADMIN_USERNAME, process.env.TEST_ADMIN_PASSWORD);

  // 为后续任务与跑腿员流程注册完全隔离的普通用户。
  await user.goto(`${baseUrl}/Auth/Register`, { waitUntil: "networkidle" });
  await user.locator("#Username").fill(process.env.TEST_USER_USERNAME);
  await user.locator("#Phone").fill(process.env.TEST_USER_PHONE);
  await user.locator("#Password").fill(process.env.TEST_USER_PASSWORD);
  await user.locator("#ConfirmPassword").fill(process.env.TEST_USER_PASSWORD);
  await Promise.all([
    user.waitForURL(/\/Auth\/Login/, { timeout: 15000 }),
    user.locator('form[action="/Auth/Register"] button[type="submit"]').click(),
  ]);
  await login(user, process.env.TEST_USER_USERNAME, process.env.TEST_USER_PASSWORD);

  await user.goto(`${baseUrl}/Address/Create`, { waitUntil: "networkidle" });
  await user.locator("#ContactName").fill("组员4复测用户");
  await user.locator("#ContactPhone").fill(process.env.TEST_USER_PHONE);
  await user.locator("#Campus").fill("复测校区");
  await user.locator("#BuildingRoom").fill(`${runId}测试房间`);
  await user.locator('input[name="IsDefault"]').check();
  await Promise.all([
    user.waitForURL(/\/Address(\/Index)?$/, { timeout: 15000 }),
    user.locator('form[action="/Address/Create"] button[type="submit"]').click(),
  ]);

  // TC-BASE-01：节点新增、修改、关闭、恢复，以及关闭后发布页不可选。
  await admin.goto(`${baseUrl}/Node`, { waitUntil: "networkidle" });
  await admin.locator("#show-node-editor").click();
  await admin.locator("#CreateModel_NodeType").selectOption("STATION");
  await admin.locator("#CreateModel_NodeName").fill(nodeName);
  await admin.locator("#CreateModel_Location").fill(`${runId}初始位置`);
  await admin.locator("#CreateModel_OpenTime").fill("08:00-22:00");
  await admin.locator("#CreateModel_NodeStatus").selectOption("NORMAL");
  await submitAndWait(admin, admin.locator('button[form="node-create-form"][type="submit"]'));
  let nodeRow = admin.locator("tbody tr").filter({ hasText: nodeName }).first();
  await nodeRow.waitFor();
  const nodeId = (await nodeRow.getAttribute("id")).replace("node-row-", "");
  await admin.screenshot({ path: output("TC-BASE-01-1-节点新增.png"), fullPage: true });

  await nodeRow.locator("button.edit-node").click();
  await admin.locator("#EditModel_NodeName").fill(editedNodeName);
  await admin.locator("#EditModel_Location").fill(`${runId}修改后位置`);
  await submitAndWait(admin, admin.locator('button[form="node-edit-form"][type="submit"]'));
  nodeRow = admin.locator(`#node-row-${nodeId}`);
  await admin.screenshot({ path: output("TC-BASE-01-2-节点修改.png"), fullPage: true });

  await submitAndWait(admin, nodeRow.getByRole("button", { name: "关闭", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-01-3-节点关闭.png"), fullPage: true });
  await user.goto(`${baseUrl}/Task/Create`, { waitUntil: "networkidle" });
  const closedNodeOptions = await user.locator("#NodeId option").allTextContents();
  if (closedNodeOptions.some((text) => text.includes(editedNodeName))) throw new Error("Closed node remained selectable.");
  await user.screenshot({ path: output("TC-BASE-01-4-关闭节点发布页不可选.png"), fullPage: true });
  await admin.goto(`${baseUrl}/Node`, { waitUntil: "networkidle" });
  nodeRow = admin.locator(`#node-row-${nodeId}`);
  await submitAndWait(admin, nodeRow.getByRole("button", { name: "恢复", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-01-5-节点恢复.png"), fullPage: true });

  // TC-BASE-03：服务类型新增、停用、发布页不可选、恢复启用。
  await admin.goto(`${baseUrl}/ServiceType`, { waitUntil: "networkidle" });
  await admin.locator("#show-service-type-editor").click();
  await admin.locator("#CreateModel_ServiceName").fill(serviceName);
  await admin.locator("#CreateModel_BasePrice").fill("9.99");
  await admin.locator("#CreateModel_DistanceRule").fill("复测距离附加费由调用方传入");
  await admin.locator("#CreateModel_UrgentRule").fill("复测加急附加费由调用方传入");
  await admin.locator("#CreateModel_TypeStatus").selectOption("ENABLED");
  await submitAndWait(admin, admin.locator('button[form="service-type-create-form"][type="submit"]'));
  let serviceRow = admin.locator("tbody tr").filter({ hasText: serviceName }).first();
  await serviceRow.waitFor();
  const serviceId = (await serviceRow.getAttribute("id")).replace("service-type-row-", "");
  await admin.screenshot({ path: output("TC-BASE-03-1-服务类型新增.png"), fullPage: true });
  await submitAndWait(admin, serviceRow.getByRole("button", { name: "停用", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-03-2-服务类型停用.png"), fullPage: true });
  await user.goto(`${baseUrl}/Task/Create`, { waitUntil: "networkidle" });
  const disabledServiceOptions = await user.locator("#ServiceTypeId option").allTextContents();
  if (disabledServiceOptions.some((text) => text.includes(serviceName))) throw new Error("Disabled service remained selectable.");
  await user.screenshot({ path: output("TC-BASE-03-3-停用服务发布页不可选.png"), fullPage: true });
  await admin.goto(`${baseUrl}/ServiceType`, { waitUntil: "networkidle" });
  serviceRow = admin.locator(`#service-type-row-${serviceId}`);
  await submitAndWait(admin, serviceRow.getByRole("button", { name: "启用", exact: true }));

  // TC-BASE-04：绑定、解绑、服务端拒绝未绑定组合、恢复绑定并成功发布。
  await admin.goto(`${baseUrl}/ServiceNodeRule`, { waitUntil: "networkidle" });
  await admin.locator("#show-rule-editor").click();
  await selectContaining(admin, "#CreateModel_ServiceTypeId", serviceName);
  await selectContaining(admin, "#CreateModel_NodeId", editedNodeName);
  await submitAndWait(admin, admin.locator('button[form="rule-create-form"][type="submit"]'));
  let ruleRow = admin.locator("tbody tr").filter({ hasText: serviceName }).filter({ hasText: editedNodeName }).first();
  await ruleRow.waitFor();
  await admin.screenshot({ path: output("TC-BASE-04-1-服务节点绑定.png"), fullPage: true });
  admin.once("dialog", (dialog) => dialog.accept());
  await submitAndWait(admin, ruleRow.getByRole("button", { name: "解除", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-04-2-服务节点解绑.png"), fullPage: true });

  async function fillTask(title) {
    await user.goto(`${baseUrl}/Task/Create`, { waitUntil: "networkidle" });
    await user.locator("#task-kind-select").selectOption("FOOD");
    await user.locator("#ServiceTypeId").selectOption(serviceId);
    await user.locator("#NodeId").selectOption(nodeId);
    await user.locator("#TaskTitle").fill(title);
    await user.locator("#TaskPrice").fill("9.99");
    await user.locator("#MerchantName").fill("组员4复测商家");
  }

  await fillTask(`${runId}未绑定组合`);
  await submitAndWait(user, user.locator('form[action="/Task/Create"] button[type="submit"]'));
  if (!(await user.locator("body").innerText()).includes("不匹配")) throw new Error("Unbound service/node pair was not rejected.");
  await user.screenshot({ path: output("TC-BASE-04-3-解绑后发布拒绝.png"), fullPage: true });

  await admin.goto(`${baseUrl}/ServiceNodeRule`, { waitUntil: "networkidle" });
  await admin.locator("#show-rule-editor").click();
  await admin.locator("#CreateModel_ServiceTypeId").selectOption(serviceId);
  await admin.locator("#CreateModel_NodeId").selectOption(nodeId);
  await submitAndWait(admin, admin.locator('button[form="rule-create-form"][type="submit"]'));

  await fillTask(`${runId}绑定成功任务`);
  await submitAndWait(user, user.locator('form[action="/Task/Create"] button[type="submit"]'));
  if (!(await user.locator("body").innerText()).includes("任务发布成功")) throw new Error("Bound service/node pair did not publish successfully.");
  await user.screenshot({ path: output("TC-BASE-04-4-绑定后发布成功.png"), fullPage: true });

  // TC-BASE-02：现在节点已被本轮任务引用，删除必须被保护。
  await admin.goto(`${baseUrl}/Node`, { waitUntil: "networkidle" });
  nodeRow = admin.locator(`#node-row-${nodeId}`);
  admin.once("dialog", (dialog) => dialog.accept());
  await submitAndWait(admin, nodeRow.getByRole("button", { name: "删除", exact: true }));
  if (!(await admin.locator("body").innerText()).includes("不能删除")) throw new Error("Referenced node deletion was not rejected.");
  await admin.screenshot({ path: output("TC-BASE-02-使用中节点删除保护.png"), fullPage: true });

  // TC-BASE-05：普通用户提交跑腿员申请。
  await user.goto(`${baseUrl}/Runner/Apply`, { waitUntil: "networkidle" });
  await user.locator("#Form_RealName").fill(`${runId}跑腿员`);
  await user.locator("#Form_IdentityInfo").fill(`M4-TEST-${runId}`);
  await submitAndWait(user, user.locator('form[action="/Runner/Apply"] button[type="submit"]'));
  if (!(await user.locator("body").innerText()).includes("待审核")) throw new Error("Runner application did not become pending.");
  await user.screenshot({ path: output("TC-BASE-05-跑腿员申请待审核.png"), fullPage: true });

  // TC-BASE-07 前半：管理员驳回，用户重新提交。
  await admin.goto(`${baseUrl}/Runner/Pending`, { waitUntil: "networkidle" });
  let runnerRow = admin.locator("tbody tr").filter({ hasText: process.env.TEST_USER_USERNAME }).first();
  await runnerRow.waitFor();
  admin.once("dialog", (dialog) => dialog.accept());
  await submitAndWait(admin, runnerRow.getByRole("button", { name: "拒绝", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-07-1-申请被驳回.png"), fullPage: true });
  await user.goto(`${baseUrl}/Runner/Apply`, { waitUntil: "networkidle" });
  await user.locator("#Form_RealName").fill(`${runId}跑腿员复申`);
  await user.locator("#Form_IdentityInfo").fill(`M4-REAPPLY-${runId}`);
  await submitAndWait(user, user.locator('form[action="/Runner/Apply"] button[type="submit"]'));
  await user.screenshot({ path: output("TC-BASE-07-2-驳回后重新提交.png"), fullPage: true });

  // TC-BASE-06 与 TC-BASE-07 后半：管理员审核通过。
  await admin.goto(`${baseUrl}/Runner/Pending`, { waitUntil: "networkidle" });
  runnerRow = admin.locator("tbody tr").filter({ hasText: process.env.TEST_USER_USERNAME }).first();
  await submitAndWait(admin, runnerRow.getByRole("button", { name: "通过", exact: true }));
  await admin.screenshot({ path: output("TC-BASE-06-管理员审核通过.png"), fullPage: true });
  await admin.screenshot({ path: output("TC-BASE-07-3-复审通过.png"), fullPage: true });

  // TC-BASE-08：新会话重新登录，确认 RUNNER 能进入任务大厅。
  const runnerContext = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const runnerPage = await runnerContext.newPage();
  await login(runnerPage, process.env.TEST_USER_USERNAME, process.env.TEST_USER_PASSWORD);
  const hallResponse = await runnerPage.goto(`${baseUrl}/Task/Hall`, { waitUntil: "networkidle" });
  if (hallResponse.status() !== 200 || runnerPage.url().includes("AccessDenied")) throw new Error("Approved runner cannot access task hall.");
  await runnerPage.screenshot({ path: output("TC-BASE-08-审核通过账号任务大厅.png"), fullPage: true });

  console.log(`PASS TC-BASE-01..08 run=${runId} user=${process.env.TEST_USER_USERNAME} node_id=${nodeId} service_type_id=${serviceId}`);
  await browser.close();
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
