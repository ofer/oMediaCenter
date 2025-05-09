import { ChangeDetectorRef, Component } from '@angular/core';
import { MediaDataService } from '../media-data.service';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';
import { MediaFileRecord } from '../media-file-record';
import { Router } from '@angular/router';

@Component({
  selector: 'app-recently-played-page',
  templateUrl: './recently-played-page.component.html',
  styleUrls: ['./recently-played-page.component.css']
})
export class RecentlyPlayedPageComponent {
  title = 'media List';
  mediaFileList!: GroupedMediaFileRecords[];

  constructor(
    private mediaDataService: MediaDataService,
    private router: Router,
    private ref: ChangeDetectorRef
  ) {
    console.log('MediaListComponent constructor');
    this.mediaDataService.getGroupedMediaFileRecords()
      .then(fileRecords => {
        this.mediaFileList = this.sortAndFilter(fileRecords);
        
      });
  }

  ngOnInit(): void {
    console.log('MediaListComponent ngOnInit');
    setTimeout(() => {
      // require view to be updated
      console.log('MediaListComponent markForCheck');
      this.ref.markForCheck();
      
    }, 1000);


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

  sortAndFilter(fileRecords: GroupedMediaFileRecords[]): GroupedMediaFileRecords[] {
    fileRecords = fileRecords.filter(gmfr => {
      for (const mfr of gmfr.mediaFileRecords) {
        if (mfr.lastPlayedDate) {
          return true;
        }
      }
      return false;
  });
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
    
    this.router.navigate(['/media', media.hash]);
  }

}
