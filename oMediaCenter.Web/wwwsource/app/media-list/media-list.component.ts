import { Component, OnInit } from '@angular/core';
import { MediaFileRecord } from '../media-file-record';
import { MediaDataService } from '../media-data.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-media-list',
  templateUrl: './media-list.component.html',
  styleUrls: ['./media-list.component.css']
})
export class MediaListComponent implements OnInit {
  title = 'media List';
  mediaFileList: MediaFileRecord[];

  constructor(
      private mediaDataService: MediaDataService,
      private router: Router
  ) { }

  ngOnInit(): void {
      this.mediaDataService.getMediaFileRecords()
          .then(fileRecords =>
              this.mediaFileList = fileRecords);
  }

  onSelect(media: MediaFileRecord) {
      this.router.navigate(['/media', media.hash]);
  }
}
