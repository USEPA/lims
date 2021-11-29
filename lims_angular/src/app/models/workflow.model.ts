export interface Workflow {
  id: string;
  name: string;
  processor: string;
  inputFolder: string;
  outputFolder: string;
  archiveFolder: string;
  interval: number;
  active: boolean;
}
