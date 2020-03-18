import { User } from "./user.model";

export interface Task {
  id: string;
  taskID: string;
  start: string;
  filePath: string;
  processor: string;
  workflowID: string;
  status: string;
  error: string;
}
