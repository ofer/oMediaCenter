import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideRouter } from '@angular/router';
import { MediaListComponent } from './media-list.component';
import { MediaDataService } from '../media-data.service';
import { ClientControlService } from '../client-control.service';
import { Subject } from 'rxjs';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';

describe('MediaListComponent', () => {
  let component: MediaListComponent;
  let fixture: ComponentFixture<MediaListComponent>;
  let mediaListUpdatedSource: Subject<void>;
  let mediaDataService: jasmine.SpyObj<MediaDataService>;

  beforeEach(waitForAsync(() => {
    mediaListUpdatedSource = new Subject<void>();
    mediaDataService = jasmine.createSpyObj<MediaDataService>('MediaDataService', ['getGroupedMediaFileRecords']);
    mediaDataService.getGroupedMediaFileRecords.and.returnValue(Promise.resolve([] as GroupedMediaFileRecords[]));

    TestBed.configureTestingModule({
      declarations: [MediaListComponent],
      providers: [
        { provide: MediaDataService, useValue: mediaDataService },
        { provide: ClientControlService, useValue: { mediaListUpdated$: mediaListUpdatedSource.asObservable() } },
        provideRouter([])
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('refreshes when media list update notification arrives', async () => {
    await fixture.whenStable();
    expect(mediaDataService.getGroupedMediaFileRecords).toHaveBeenCalledTimes(1);

    mediaListUpdatedSource.next();
    await fixture.whenStable();

    expect(mediaDataService.getGroupedMediaFileRecords).toHaveBeenCalledTimes(2);
  });
});
