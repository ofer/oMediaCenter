import { Component, OnInit } from '@angular/core';
import { MediaFileRecord } from '../media-file-record';
import { MediaDataService } from '../media-data.service';
import { Router } from '@angular/router';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';

@Component({
  selector: 'app-media-list',
  templateUrl: './media-list.component.html',
  styleUrls: ['./media-list.component.css']
})
export class MediaListComponent implements OnInit {
  title = 'media List';
  mediaFileList!: GroupedMediaFileRecords[];

  constructor(
    private mediaDataService: MediaDataService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.mediaDataService.getGroupedMediaFileRecords()
      .then(fileRecords =>
        this.mediaFileList = this.sort(fileRecords));
  }

  getSeasons(fileList: MediaFileRecord[]): MediaFileRecord[][] {
    let result: MediaFileRecord[][] = [];

    let lastSeasonNumber = fileList[0].season;
    let seasonEpisodes: MediaFileRecord[] = [];
    for (const file of fileList) {
      if (lastSeasonNumber != file.season)
      {
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
