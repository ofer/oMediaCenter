import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { MediaFileRecord } from '../media-file-record';
import { MediaDataService } from '../media-data.service';
import { Router } from '@angular/router';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';

@Component({
    selector: 'app-media-list',
    templateUrl: './media-list.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./media-list.component.css'],
    standalone: false
})
/**
 * Represents a component for displaying a list of media files.
 * Allows users to navigate through the list using keyboard shortcuts.
 * Emits an event when a media file is selected.
 */
export class MediaListComponent implements OnInit, OnDestroy {
  title = 'media List';
  mediaFileList!: GroupedMediaFileRecords[];
  @Output() mediaSelected: EventEmitter<string> = new EventEmitter<string>();
  selectedMediaIndex: number = 0;

  keyHandler: any;


  constructor(
    private mediaDataService: MediaDataService,
    private router: Router,
    private ref: ChangeDetectorRef
  ) {
    console.log('MediaListComponent constructor');
    this.mediaDataService.getGroupedMediaFileRecords()
      .then(fileRecords => {
        this.mediaFileList = this.sort(fileRecords);
        console.log('MediaListComponent getGroupedMediaFileRecords');
        setTimeout(() => {
          // require view to be updated
          console.log('MediaListComponent markForCheck');
          this.ref.markForCheck();

        }, 1000);

      });
    this.keyHandler = (event: KeyboardEvent) => {
      // Handle keypress event here
      this.HandleKeydown(event);
    };

    document.addEventListener('keydown', this.keyHandler);
  }

  HandleKeydown(event: KeyboardEvent) {
    console.log('MediaListComponent HandleKeypress', event.key);

    let handlerFound = true;
    switch (event.key) {
      case 'Enter':
        this.onSelect(this.mediaFileList[this.selectedMediaIndex].mediaFileRecords[0]);
        break;
      case 'ArrowRight':
        do {
          if (this.selectedMediaIndex < this.mediaFileList.length - 1) {
            this.selectedMediaIndex++;
          }
          else {
            this.selectedMediaIndex = this.mediaFileList.length - 1;
            break;
          }
        }
        while (this.setFocus() != true);
        break;
      case 'ArrowLeft':
        do {
          if (this.selectedMediaIndex > 0) {
            this.selectedMediaIndex--;
          }
          else {
            this.selectedMediaIndex = 0;
            break;
          }
        }
        while (this.setFocus() != true);
        break;
      case 'ArrowUp':
        if (this.selectedMediaIndex > 0) {
          this.setToPreviousRowIndex();
          this.setFocus();
        }
        break;
      case 'ArrowDown':
        if (this.selectedMediaIndex < this.mediaFileList.length - 1) {
          this.setToNextRowIndex();
          this.setFocus();
        }
        break;
      default:
        handlerFound = false;
    }

    if (handlerFound) {
      event.preventDefault(); // Prevent default action for handled keys
    }
  }

  setToPreviousRowIndex() {
    let mediaElement = document.querySelector('#movie-' + this.selectedMediaIndex);

    if (!mediaElement) {
      console.warn('Media element not found for index:' + this.selectedMediaIndex);
      return;
    }

    let rect = mediaElement.getBoundingClientRect();
    let currentTop = rect.top;
    let nextTop = currentTop;

    let countToPreviousRow = 0;

    while (nextTop === currentTop) {
      if (this.selectedMediaIndex > 0) {
        this.selectedMediaIndex--;
        countToPreviousRow++;
        while ((mediaElement = document.querySelector('#movie-' + this.selectedMediaIndex)) == null) {
          this.selectedMediaIndex--;
          countToPreviousRow++;
          if (this.selectedMediaIndex < 0) {
            break;
          }
        }
        rect = (mediaElement as Element).getBoundingClientRect();
        nextTop = rect.top;
      } else {
        break;
      }
    }
    this.setFocus();
  }

  setToNextRowIndex() {
    let mediaElement = document.querySelector('#movie-' + this.selectedMediaIndex);

    if (!mediaElement) {
      console.warn('Media element not found for index:', this.selectedMediaIndex);
      return;
    }

    let rect = mediaElement.getBoundingClientRect();
    let currentTop = rect.top;
    let nextTop = currentTop;

    let countToNextRow = 0;

    while (nextTop === currentTop) {
      if (this.selectedMediaIndex < this.mediaFileList.length - 1) {
        this.selectedMediaIndex++;
        countToNextRow++;
        while ((mediaElement = document.querySelector('#movie-' + this.selectedMediaIndex)) == null) {
          this.selectedMediaIndex++;
          countToNextRow++;
          if (this.selectedMediaIndex >= this.mediaFileList.length) {
            break;
          }
        }
        rect = (mediaElement as Element).getBoundingClientRect();
        nextTop = rect.top;
      } else {
        break;
      }
    }
    this.setFocus();
  }

  setFocus(): boolean {
    const mediaElement = document.querySelector('#movie-' + this.selectedMediaIndex);
    if (mediaElement) {
      mediaElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      (mediaElement as HTMLButtonElement).focus();

      return true;
    }
    return false;
  }

  ngOnInit(): void {
    console.log('MediaListComponent ngOnInit');
    setTimeout(() => {
      // require view to be updated
      console.log('MediaListComponent markForCheck');
      this.ref.markForCheck();

    }, 1000);
  }

  ngOnDestroy(): void {
    document.removeEventListener('keydown', this.keyHandler);
  }

  getSeasons(fileList: MediaFileRecord[]): MediaFileRecord[][] {
    console.log('MediaListComponent getSeasons');
    let result: MediaFileRecord[][] = [];

    let lastSeasonNumber = fileList[0].season;
    let seasonEpisodes: MediaFileRecord[] = [];
    for (const file of fileList) {
      if (lastSeasonNumber != file.season) {
        result.push(seasonEpisodes);
        seasonEpisodes = [];
        lastSeasonNumber = file.season;
      }
      seasonEpisodes.push(file);
    }

    result.push(seasonEpisodes);
    return result;
  }

  sort(fileRecords: GroupedMediaFileRecords[]): GroupedMediaFileRecords[] {
    console.log('MediaListComponent sort');
    fileRecords.sort((gmfra, gmfrb) => {
      let nameComparison = gmfra.name.localeCompare(gmfrb.name);
      return nameComparison;
    });
    for (const groupRecord of fileRecords) {
      groupRecord.mediaFileRecords.sort((mfra, mfrb) => {
        let nameComparison = mfra.name.localeCompare(mfrb.name);
        if (nameComparison == 0) {
          if (mfra.year != mfrb.year)
            return mfra.year > mfrb.year ? 1 : -1;
          if (mfra.season != mfrb.season)
            return mfra.season > mfrb.season ? 1 : -1;
          if (mfra.episode != mfrb.episode)
            return mfra.episode > mfrb.episode ? 1 : -1;
        } else {
          return nameComparison;
        }

        return 0;
      });
    }
    return fileRecords;
  }

  onSelect(media: MediaFileRecord) {
    this.mediaSelected.emit(media.hash);
  }
}
