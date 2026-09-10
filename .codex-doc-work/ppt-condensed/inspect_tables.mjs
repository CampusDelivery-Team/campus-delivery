import { FileBlob, PresentationFile } from '@oai/artifact-tool';
const source = 'D:/delivery-backend/docs/校园综合跑腿与代取服务管理系统-课程设计答辩.pptx';
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
for (const index of [12, 41]) {
  const slide = presentation.slides.getItem(index);
  const preview = await slide.export({ format: 'layout' });
  console.log(`SLIDE ${index + 1}\n${await preview.text()}`);
}
