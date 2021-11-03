export interface Workflow {
  id: string;
  name: string;
  processor: string;
  inputFolder: string;
  outputFolder: string;
  backupFolder: string;
  interval: number;
  active: boolean;
}
