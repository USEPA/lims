import { TestBed } from '@angular/core/testing';

import { TaskManagerService } from './task-manager.service';

describe('FileManagerService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: TaskManagerService = TestBed.get(TaskManagerService);
    expect(service).toBeTruthy();
  });
});
