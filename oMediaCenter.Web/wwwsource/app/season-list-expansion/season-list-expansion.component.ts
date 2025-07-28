import { Component, OnInit, Input, EventEmitter, Output, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { MediaFileRecord } from '../media-file-record';

@Component({
  selector: 'app-season-list-expansion',
  templateUrl: './season-list-expansion.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['./season-list-expansion.component.css']
})
export class SeasonListExpansionComponent implements OnInit, OnDestroy {
  @Input() seasonFileList: MediaFileRecord[] | null = null;
  @Output() mediaSelected: EventEmitter<string> = new EventEmitter<string>();

  shouldShowFileList: boolean;
  SeasonNumber() {
    get: {
      if (this.seasonFileList && this.seasonFileList.length > 0) {
        return this.seasonFileList[0].season;
      }
      return 'Unknown';
    }
  };

  constructor(private ref: ChangeDetectorRef) {
    this.shouldShowFileList = false;
    console.log('In constructor');
  }

  ngOnDestroy(): void {
    console.log('In ngOnDestroy');
  }

  ngOnInit(): void {
    // setTimeout(() => {
    //   // require view to be updated
    //   console.log('MediaListComponent markForCheck');
    //   this.ref.markForCheck();

    // }, 1000);

  }

  toggleShowFileList() {
    console.log('Toggling file list visibility');
    this.shouldShowFileList = !this.shouldShowFileList;
    console.log('Finished toggling file list visibility');
    setTimeout(() => {
      // require view to be updated
      console.log('MediaListComponent markForCheck');
      this.ref.markForCheck();

    }, 1000);
  }

  onSelect(media: MediaFileRecord) {
    console.log('Selected media: ', media);
    this.mediaSelected.emit(media.hash);
    // this.router.navigate(['/media', media.hash]);
  }

}
