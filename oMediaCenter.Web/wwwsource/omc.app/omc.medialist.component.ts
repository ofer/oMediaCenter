import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, Params } from '@angular/router';

import { MediaFileRecord } from './omc.mediafilerecord.model';
import { MediaDataService } from './omc.media.service';


@Component({
    selector: 'omc-medialist',
    templateUrl: './omc.app/omc.medialist.component.html',
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
