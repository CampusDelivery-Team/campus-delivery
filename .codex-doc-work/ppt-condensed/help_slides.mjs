import { FileBlob, PresentationFile } from '@oai/artifact-tool';
const source = 'D:/delivery-backend/docs/校园综合跑腿与代取服务管理系统-课程设计答辩.pptx';
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
console.log(JSON.stringify(presentation.help('slides delete remove move duplicate', { maxChars: 8000 }), null, 2));
