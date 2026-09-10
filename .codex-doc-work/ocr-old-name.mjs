import { createWorker } from './ppt-name-update/node_modules/tesseract.js/src/index.js';

const worker = await createWorker('chi_sim');
for (const imagePath of process.argv.slice(2)) {
  const { data } = await worker.recognize(imagePath);
  const hits = data.text.split(/\r?\n/).filter(line => line.includes('校园') || line.includes('中转') || line.includes('跑腿'));
  console.log(`${imagePath}: ${hits.join(' | ')}`);
}
await worker.terminate();
