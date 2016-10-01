import { Component, OnInit } from '@angular/core';

import { MediaFileRecord } from './omc.mediafilerecord.model';

import { MediaDataService } from './omc.media.service';


@Component({
    selector: 'omc-medialist',
    templateUrl: './omc.app/omc.medialist.component.html',
})
export class MediaListComponent implements OnInit {
    title = 'Tour of zeros';
    mediaFileList: MediaFileRecord[];

    constructor(
        private mediaDataService: MediaDataService
    ) { }

    ngOnInit(): void {
        //this.mediaDataService.getMediaFileRecords().then(fileRecords => this.mediaFileList = fileRecords);
    }
}
